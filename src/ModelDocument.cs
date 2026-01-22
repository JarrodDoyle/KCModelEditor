using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KeepersCompound.Formats.Model;
using Serilog;

namespace KeepersCompound.ModelEditor;

public class ModelDocument
{
    #region Events

    public delegate void ActionDoneEventHandler();

    public event ActionDoneEventHandler? ActionDone;

    #endregion

    public bool Dirty { get; private set; }
    public string Name { get; }
    public string Campaign { get; }
    public ModelFile Model { get; }

    private readonly Stack<(Action<ModelFile>, Action<ModelFile>)> _undo = new();
    private readonly Stack<(Action<ModelFile>, Action<ModelFile>)> _redo = new();

    public ModelDocument(ModelFile model, string name, string campaign)
    {
        Model = model;
        Name = name;
        Campaign = campaign;
        Dirty = false;
    }

    public void Save(string path)
    {
        var parser = new ModelFileParser();
        using var outStream = File.Open(path, FileMode.Create);
        using var writer = new BinaryWriter(outStream, Encoding.UTF8, false);
        parser.Write(writer, Model);
        Log.Information("Saved model to {path}", path);

        _undo.Clear();
        _redo.Clear();
        Dirty = false;
    }

    public void DoAction((Action<ModelFile>, Action<ModelFile>) action)
    {
        Log.Debug("Doing");
        action.Item1(Model);
        _undo.Push(action);
        _redo.Clear();
        Dirty = true;
        ActionDone?.Invoke();
    }

    public bool UndoAction()
    {
        Log.Debug("Undoing");
        if (!_undo.TryPop(out var action))
        {
            Log.Debug("Nothing left to undo...");
            return false;
        }

        action.Item2(Model);
        _redo.Push(action);
        ActionDone?.Invoke();
        Dirty = _undo.Count != 0;
        return true;
    }

    public bool RedoAction()
    {
        Log.Debug("Redoing");
        if (!_redo.TryPop(out var action))
        {
            Log.Debug("Nothing left to redo...");
            return false;
        }

        action.Item1(Model);
        _undo.Push(action);
        ActionDone?.Invoke();
        Dirty = true;
        return true;
    }
}