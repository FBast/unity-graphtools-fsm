using System;
using UnityEditor.Experimental.GraphView;

namespace GraphToolsFSM.Editor.Core
{
    public class CategorizedPort : Port {
        public PortCategory Category;

        public CategorizedPort(Orientation orientation, Direction direction, Capacity capacity, Type type, PortCategory category)
            : base(orientation, direction, capacity, type)
        {
            Category = category;
        }

        public static CategorizedPort Create(Orientation orientation, Direction direction, Capacity capacity, Type type, PortCategory category)
        {
            var port = new CategorizedPort(orientation, direction, capacity, type, category);
            return port;
        }
    }
    
    [Flags]
    public enum PortCategory {
        None = 0,
        State = 1 << 0,
        Condition = 1 << 1,
        Logical = 1 << 2,
        All = State | Condition | Logical,
    }
}