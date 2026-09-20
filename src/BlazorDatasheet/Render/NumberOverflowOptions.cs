namespace BlazorDatasheet.Render;

/// <summary>
/// Controls numeric overflow. General numbers may round down to MinDecimals decimal places;
/// explicitly formatted numbers retain their complete format.
/// </summary>
public readonly record struct NumberOverflowOptions(NumberOverflowMode Mode, int MinDecimals);
