using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.FormulaEngine;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Dependencies;
using CellFormula = BlazorDatasheet.Formula.Core.Interpreter.CellFormula;

namespace BlazorDatasheet.Core.Data.Cells;

internal class CellStoreRestoreData
{
    private MatrixRestoreData<CellValue>? _valueRestoreData;

    internal MatrixRestoreData<CellValue> ValueRestoreData
    {
        get
        {
            if (_valueRestoreData == null)
                _valueRestoreData = new();
            return _valueRestoreData;
        }
        set => _valueRestoreData = value;
    }

    private MatrixRestoreData<CellFormula?>? _formulaRestoreData;

    internal MatrixRestoreData<CellFormula?> FormulaRestoreData
    {
        get
        {
            if (_formulaRestoreData == null)
                _formulaRestoreData = new();
            return _formulaRestoreData;
        }
        set => _formulaRestoreData = value;
    }

    private MatrixRestoreData<bool?>? _validRestoreData;

    internal MatrixRestoreData<bool?> ValidRestoreData
    {
        get
        {
            if (_validRestoreData == null)
                _validRestoreData = new();
            return _validRestoreData;
        }
        set => _validRestoreData = value;
    }

    private RegionRestoreData<string>? _typeRestoreData;

    internal RegionRestoreData<string> TypeRestoreData
    {
        get
        {
            if (_typeRestoreData == null)
                _typeRestoreData = new();
            return _typeRestoreData;
        }
        set => _typeRestoreData = value;
    }

    private RegionRestoreData<CellFormat>? _formatRestoreData;

    internal RegionRestoreData<CellFormat> FormatRestoreData
    {
        get
        {
            if (_formatRestoreData == null)
                _formatRestoreData = new();
            return _formatRestoreData;
        }
        set => _formatRestoreData = value;
    }

    private RegionRestoreData<bool>? _mergeRestoreData;

    internal RegionRestoreData<bool> MergeRestoreData
    {
        get
        {
            if (_mergeRestoreData == null)
                _mergeRestoreData = new();
            return _mergeRestoreData;
        }
        set => _mergeRestoreData = value;
    }

    private DependencyManagerRestoreData? _dependencyGraph;

    internal DependencyManagerRestoreData DependencyManagerRestoreData
    {
        get
        {
            if (_dependencyGraph == null)
                _dependencyGraph = new();
            return _dependencyGraph;
        }
        set => _dependencyGraph = value;
    }

    internal IEnumerable<CellPosition> GetAffectedPositions()
    {
        return ValidRestoreData.DataRemoved.Select(x => new CellPosition(x.row, x.col))
            .Concat(ValueRestoreData.DataRemoved.Select(x => new CellPosition(x.row, x.col))
                .Concat(FormulaRestoreData.DataRemoved.Select(x => new CellPosition(x.row, x.col))));
    }

    internal IEnumerable<IRegion> GetAffectedRegions()
    {
        return TypeRestoreData.RegionsAdded.Select(x => x.Region)
            .Concat(TypeRestoreData.RegionsRemoved.Select(x => x.Region))
            .Concat(FormatRestoreData.RegionsAdded.Select(x => x.Region))
            .Concat(FormatRestoreData.RegionsRemoved.Select(x => x.Region));
    }

    internal void Merge(CellStoreRestoreData item)
    {
        if (item._valueRestoreData != null)
            ValueRestoreData.Merge(item._valueRestoreData);

        if (item._formulaRestoreData != null)
            FormulaRestoreData.Merge(item._formulaRestoreData);

        if (item._validRestoreData != null)
            ValidRestoreData.Merge(item.ValidRestoreData);

        if (item._typeRestoreData != null)
            TypeRestoreData.Merge(item._typeRestoreData);

        if (item._formatRestoreData != null)
            FormatRestoreData.Merge(item._formatRestoreData);

        if (item._mergeRestoreData != null)
            MergeRestoreData.Merge(item.MergeRestoreData);

        if (item._dependencyGraph != null)
            DependencyManagerRestoreData.Merge(item._dependencyGraph);
    }
}