namespace SceneGate.Hinox.Utils;

internal record ExportedFileInfo(string Path, long Offset, long OriginalLength)
{
    public ExportedFileInfo()
        : this(string.Empty, 0, -1)
    {
    }
}
