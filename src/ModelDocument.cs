using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KeepersCompound.Formats.Model;
using Serilog;
using Serilog.Debugging;

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

    public void TriggerActionDone()
    {
        ActionDone?.Invoke();
    }
}