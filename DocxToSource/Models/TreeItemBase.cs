using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DocxToSource.Models;

public abstract class TreeItemBase
{
    protected readonly CodeGeneratorOptions Cgo = new() { BracingStyle = "C" };
    public List<TreeItemBase> Items { get; set; }
    public string Header { get; set; }
    public abstract string BuildCodeDomTextDocument(CodeDomProvider provider);
    public virtual string BuildXmlTextDocument()
    {
        return String.Empty;
    }

    protected TreeItemBase()
    {
        Items = new List<TreeItemBase>();
    }
    
    public List<OpenXmlTreeItem> BuildXmlElementTreeItem(IEnumerable<OpenXmlElement> elements)
    {
        var output = new List<OpenXmlTreeItem>(elements.Count());
        if (elements is null || !elements.Any())
        {
            return output;
        }
        string header;
        Row row;
        Cell cell;
        uint index = 0;

        foreach (OpenXmlElement e in elements)
        {
            header = $"<{index++}> {e.LocalName} ({e.GetType().Name})";
            row = e as Row;
            cell = e as Cell;

            if (row != null && row.RowIndex != null && row.RowIndex.HasValue)
            {
                header += $" [{(e as Row).RowIndex.Value}]";
            }
            else if (cell != null && cell.CellReference != null && cell.CellReference.HasValue)
            {
                header += $" [{(e as Cell).CellReference.Value}]";
            }
            output.Add(new OpenXmlTreeItem(e)
            {
                Header = header
            });
        }

        return output;
    }
}