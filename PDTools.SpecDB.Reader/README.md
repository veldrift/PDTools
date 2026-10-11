# Original-table SpecDB reader

This net9.0 library compiles the existing PDTools.SpecDB/Core sources for consumers that read player-supplied original tables. It omits the SQLite wrapper, database export CLI and native SQLite package. It shares PDTools.Utils and Syroot.BinaryData with the original reader; it does not introduce a second table implementation.

Optional Debug diagnostics use an explicit caller-owned output directory and are disposed with SpecDB. Leaving that directory null disables diagnostic file output. Release retains upstream's conditional behavior. Input tables remain read-only. PDTools' repository MIT license and Nenkai attribution apply.

The GT4 viewer validates the GT4_US2560 edition; this project does not claim every database mapping is correct. Its DEFAULT_PARTS references are consumed by embedded table IDs at the application boundary because historical column labels are shifted.
