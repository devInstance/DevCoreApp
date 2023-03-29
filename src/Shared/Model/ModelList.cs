using System;
using System.Collections.Generic;
using System.Text;

namespace DevInstance.DevCoreApp.Shared.Model;

public class ModelList<T>
{
    /// <summary>
    /// Total count of items
    /// </summary>
    public int TotalCount { get; set; }
    /// <summary>
    /// Total count of pages
    /// </summary>
    public int PagesCount { get; set; }
    /// <summary>
    /// Selected page index (starting from 0)
    /// </summary>
    public int Page { get; set; }
    /// <summary>
    /// Count of item on in selected time range
    /// </summary>
    public int Count { get; set; }
    /// <summary>
    /// Array of items
    /// </summary>
    public T[] Items { get; set; }
}
