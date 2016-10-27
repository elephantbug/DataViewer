using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;

namespace DataEngine
{
    struct DynamicSortDescription
    {
        public ListSortDirection Direction { get; set; }

        public string[] NameChain { get; set; }
        
        public PropertyInfo[] PropertyChain { get; set; }
    }

    class DynamicComparer<T> : IComparer<T>
    {
        DynamicSortDescription[] sortDescriptions;

        DynamicComparer(DynamicSortDescription[] sort_descriptions)
        {
            this.sortDescriptions = sort_descriptions;
        }

        public DynamicComparer(SortDescriptionCollection sort_descriptions)
        {
            if (sort_descriptions.Count == 0)
            {
                throw new InvalidOperationException("sortDescriptions.Count == 0");
            }

            bool id_found = false;

            for (int i = 0; i < sort_descriptions.Count; ++i)
            {
                string name = sort_descriptions[i].PropertyName;

                if (name == "Id")
                {
                    id_found = true;

                    break;
                }
            }

            int sort_descriptions_count = id_found ? sort_descriptions.Count : sort_descriptions.Count + 1;

            sortDescriptions = new DynamicSortDescription[sort_descriptions_count];

            for (int i = 0; i < sort_descriptions.Count; ++i)
            {
                string name = sort_descriptions[i].PropertyName;

                SetPropertyDescription(i, name, sort_descriptions[i].Direction);
            }

            if (!id_found)
            {
                SetPropertyDescription(sort_descriptions.Count, "Id", ListSortDirection.Ascending);
            }
        }

        void SetPropertyDescription(int i, string name, ListSortDirection direction)
        {
            sortDescriptions[i] = new DynamicSortDescription()
            {
                Direction = direction,

                NameChain = name.Split('.')
            };

            List<PropertyInfo> props = new List<PropertyInfo>();

            Type type = typeof(T);

            for (int j = 0; j < sortDescriptions[i].NameChain.Length; ++j)
            {
                PropertyInfo prop = type.GetProperty(sortDescriptions[i].NameChain[j]);

                if (prop == null)
                {
                    break;
                }

                props.Add(prop);

                type = prop.PropertyType;
            }

            sortDescriptions[i].PropertyChain = props.ToArray();
        }

        int AscendingCompare(DynamicSortDescription sort_description, T x, T y)
        {
            //initially left and right are objects of type derived from T

            object left = x;

            object right = y;

            for (int j = 0; j < sort_description.NameChain.Length; ++j)
            {
                PropertyInfo left_prop;

                PropertyInfo right_prop;

                if (j < sort_description.PropertyChain.Length)
                {
                    PropertyInfo prop = sort_description.PropertyChain[j];

                    left_prop = prop;

                    right_prop = prop;
                }
                else
                {
                    string prop_name = sort_description.NameChain[j];

                    left_prop = left.GetType().GetProperty(prop_name);

                    right_prop = right.GetType().GetProperty(prop_name);
                }

                left = left_prop != null ? left_prop.GetValue(left, null) : null;

                right = right_prop != null ? right_prop.GetValue(right, null) : null;

                if (left == null || right == null)
                {
                    if (left == null && right == null)
                    {
                        return 0;
                    }
                    
                    if (left == null)
                    {
                        return -1; //nulls goes first
                    }

                    return 1;
                }
            }

            IComparable left_comp = left as IComparable;

            return left_comp.CompareTo(right);
        }
        
        public int Compare(T x, T y)
        {
            for (int i = 0; i < sortDescriptions.Length; ++i)
            {
                int result = AscendingCompare(sortDescriptions[i], x, y);

                if (result != 0)
                {
                    return sortDescriptions[i].Direction == ListSortDirection.Ascending ? result : -result;
                }
            }
            
            return 0;
        }
    }
}