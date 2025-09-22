using System;
using System.Collections.Generic;
using System.Linq;
using CobanaEnergy.Project.Service;

namespace CobanaEnergy.Project.Extensions
{
    /// <summary>
    /// Extension methods for IQueryable to support DataTable sorting
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Apply DataTable sorting to a queryable using column mappings
        /// </summary>
        public static IQueryable<T> ApplyDataTableSorting<T>(
            this IQueryable<T> query, 
            DataTableHelperService.DataTableRequest request, 
            Dictionary<string, Func<IQueryable<T>, bool, IQueryable<T>>> columnMappings)
        {
            if (request.Order?.Any() == true)
            {
                var primaryOrder = request.Order.First();
                var columnName = GetColumnName(primaryOrder.Column, columnMappings.Keys.ToArray());
                
                // Only apply sorting if the column mapping exists and it's not an action column
                if (columnMappings.ContainsKey(columnName) && !IsActionColumn(primaryOrder.Column))
                {
                    var isAscending = primaryOrder.Dir == "asc";
                    query = columnMappings[columnName](query, isAscending);
                    return query;
                }
            }
            
            // If no valid sorting was applied, return the query as-is
            return query;
        }
        
        /// <summary>
        /// Apply DataTable sorting with default fallback column
        /// </summary>
        public static IQueryable<T> ApplyDataTableSorting<T>(
            this IQueryable<T> query, 
            DataTableHelperService.DataTableRequest request, 
            Dictionary<string, Func<IQueryable<T>, bool, IQueryable<T>>> columnMappings,
            string defaultColumn,
            bool defaultAscending = true)
        {
            if (request.Order?.Any() == true)
            {
                var primaryOrder = request.Order.First();
                var columnName = GetColumnName(primaryOrder.Column, columnMappings.Keys.ToArray());
                
                // Only apply sorting if the column mapping exists and it's not an action column
                if (columnMappings.ContainsKey(columnName) && !IsActionColumn(primaryOrder.Column))
                {
                    var isAscending = primaryOrder.Dir == "asc";
                    query = columnMappings[columnName](query, isAscending);
                    return query;
                }
            }
            
            // Apply default sorting if no valid order specified or if action column was clicked
            if (columnMappings.ContainsKey(defaultColumn))
            {
                query = columnMappings[defaultColumn](query, defaultAscending);
            }
            
            return query;
        }

        /// <summary>
        /// Apply DataTable sorting with column names array and default fallback column
        /// </summary>
        public static IQueryable<T> ApplyDataTableSorting<T>(
            this IQueryable<T> query, 
            DataTableHelperService.DataTableRequest request, 
            Dictionary<string, Func<IQueryable<T>, bool, IQueryable<T>>> columnMappings,
            string[] columnNames,
            string defaultColumn,
            bool defaultAscending = true)
        {
            if (request.Order?.Any() == true)
            {
                var primaryOrder = request.Order.First();
                var columnName = GetColumnName(primaryOrder.Column, columnNames);
                
                // Only apply sorting if the column mapping exists and it's not an action column
                if (columnMappings.ContainsKey(columnName) && !IsActionColumn(primaryOrder.Column))
                {
                    var isAscending = primaryOrder.Dir == "asc";
                    query = columnMappings[columnName](query, isAscending);
                    return query;
                }
            }
            
            // Apply default sorting if no valid order specified or if action column was clicked
            if (columnMappings.ContainsKey(defaultColumn))
            {
                query = columnMappings[defaultColumn](query, defaultAscending);
            }
            
            return query;
        }
        
        private static string GetColumnName(int columnIndex, string[] availableColumns)
        {
            // If we have a mapping for this index, use it
            if (columnIndex < availableColumns.Length)
            {
                return availableColumns[columnIndex];
            }
            
            // Fallback to a default column
            return availableColumns.FirstOrDefault() ?? "Agent";
        }
        
        private static bool IsActionColumn(int columnIndex)
        {
            // Action columns are typically at index 0 and contain buttons/links
            // This can be extended to handle multiple action columns if needed
            return columnIndex == 0;
        }
    }
}