using System.Diagnostics.CodeAnalysis;
using KeepersCompound.Dark;
using KeepersCompound.Dark.Resources;

namespace KeepersCompound.ModelEditor;

public class EditorState
{
    #region Events

    public delegate void ActiveModelChangedEventHandler(ModelDocument document);

    public event ActiveModelChangedEventHandler? ActiveModelChanged;

    #endregion

    public EditorConfig Config { get; }
    public InstallContext Context { get; }
    public ResourceManager Resources { get; }

    private ModelDocument? _document;

    public EditorState(EditorConfig config, InstallContext context)
    {
        Config = config;
        Context = context;
        Resources = new ResourceManager(context);
    }

    public void SetDocument(ModelDocument document)
    {
        _document = document;
        ActiveModelChanged?.Invoke(_document);
    }

    public bool TryGetDocument([MaybeNullWhen(false)] out ModelDocument document)
    {
        document = _document;
        return _document != null;
    }
}