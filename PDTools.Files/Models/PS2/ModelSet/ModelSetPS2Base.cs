using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PDTools.Files.Models.PS2.RenderCommands;
using PDTools.Files.Textures.PS2;

namespace PDTools.Files.Models.PS2.ModelSet;

public abstract class ModelSetPS2Base
{
    /// <summary>
    /// Models in this model set. The game will iterate through all of these & their commands to render on every tick.
    /// </summary>
    public List<ModelPS2Base> Models { get; set; } = [];

    /// <summary>
    /// Shapes aka meshes.
    /// </summary>
    public List<PGLUshape> Shapes { get; set; } = [];

    public abstract int GetNumModels();

    public abstract List<TextureSet1> GetTextureSetList();

    public abstract uint AddShape(PGLUshape shape);

    public abstract PGLUshape GetShape(int shapeIndex);

    public abstract int GetNumShapes();

    public abstract int AddMaterial(PGLUmaterial material);

    public abstract int GetMaterialCount();

    public abstract List<PGLUmaterial> GetVariationMaterials(int varIndex);

    public abstract int GetNumVariations();

    //////////////////////////////////
    /// Interpreter for dumping
    //////////////////////////////////

    public List<DumpedLOD> DumpModelLODs(int modelIndex, string outdir)
    {
        ModelCommandShapeExtractor context = new()
        {
            ModelIndex = modelIndex,
            OutputDir = outdir
        };

        ModelPS2Base model = Models[modelIndex];

        ProcessCommands(context, model.Commands);
        return context.LODToShapes;
    }

    private void ProcessCommands(ModelCommandShapeExtractor extractor, List<ModelSetupPS2Command> cmds)
    {
        foreach (ModelSetupPS2Command command in cmds)
        {
            switch (command.Opcode)
            {
                case ModelSetupPS2Opcode.BBoxRender:
                    ProcessCommands(extractor, (command as Cmd_BBoxRender).CommandsOnRender);
                    break;

                case ModelSetupPS2Opcode.LODSelect:
                    var lodSel = command as Cmd_LODSelect;
                    for (int i = 0; i < lodSel.CommandsPerLOD.Count; i++)
                    {
                        extractor.SetLOD(i);

                        ProcessCommands(extractor, lodSel.CommandsPerLOD[i]);
                    }
                    break;

                case ModelSetupPS2Opcode.CallModelCallback:
                    var callbackCmd = command as Cmd_CallModelCallback;
                    if (callbackCmd.Parameter == ModelCallbackParameter.IsTailLampActive)
                    {
                        extractor.CurrentCallback = 0;
                        ProcessCommands(extractor, callbackCmd.Default);

                        if (callbackCmd.CommandsPerBranch.Count < 2)
                            Console.WriteLine($"Tail-lamp callback has {callbackCmd.CommandsPerBranch.Count} branches; exporting only present branches.");

                        for (int branch = 0; branch < callbackCmd.CommandsPerBranch.Count; branch++)
                        {
                            extractor.CallbackBranchIndex = branch;
                            extractor.ExtraShapeName = branch switch
                            {
                                0 => "tail_lamp_off",
                                1 => "tail_lamp_on",
                                _ => $"tail_lamp_branch{branch}",
                            };
                            ProcessCommands(extractor, callbackCmd.CommandsPerBranch[branch]);
                        }
                    }

                    extractor.ExtraShapeName = null;
                    extractor.CurrentCallback = null;
                    break;

                case ModelSetupPS2Opcode.pgluSetTexTable_Byte:
                    byte index = (command as Cmd_pgluSetTexTable_Byte).TexSetTableIndex;
                    extractor.SetTexTable(index);
                    break;

                case ModelSetupPS2Opcode.pgluCallShape_Byte:
                case ModelSetupPS2Opcode.pgluCallShape_UShort:
                    {
                        if (extractor.CurrentLOD == -1)
                        {
                            extractor.ModelName = $"model{extractor.ModelIndex}";
                        }
                        else
                        {
                            extractor.ModelName = $"model{extractor.ModelIndex}.lod{extractor.CurrentLOD}";
                        }


                        int shapeIndex = command is Cmd_pgluCallShapeByte byteCall
                            ? byteCall.ShapeIndex
                            : ((Cmd_pgluCallShape_UShort)command).ShapeIndex;
                        PGLUshapeConverted shapeData = Shapes[shapeIndex].GetShapeData();
                        shapeData.ShapeIndex = shapeIndex;
                        shapeData.RenderCommands = extractor.RenderCommandContext.GetCurrentCommandsForContext();

                        string name = $"shape{shapeIndex}";
                        if (!string.IsNullOrEmpty(extractor.ExtraShapeName))
                            name += $"_{extractor.ExtraShapeName}";

                        if (shapeData.UsesExternalTexture)
                            name += "_reflection";

                        extractor.AddShape(name, shapeData);
                    }
                    break;

                case ModelSetupPS2Opcode.pgl_53:
                    {
                        var callShape = (command as Cmd_Unk53);
                        int shapeIndex = callShape.ShapeIndex;
                        PGLUshapeConverted shapeData = Shapes[shapeIndex].GetShapeData();
                        shapeData.ShapeIndex = shapeIndex;
                        shapeData.RenderCommands = extractor.RenderCommandContext.GetCurrentCommandsForContext();

                        string name = $"shape{shapeIndex}";
                        if (!string.IsNullOrEmpty(extractor.ExtraShapeName))
                            name += $"_{extractor.ExtraShapeName}";

                        if (shapeData.UsesExternalTexture)
                            name += "_reflection";

                        extractor.AddShape(name, shapeData);
                    }
                    break;

                default:
                    extractor.RenderCommandContext.ApplyCommand(command);
                    break;
            }
        }
    }
}
