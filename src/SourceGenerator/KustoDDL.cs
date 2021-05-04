﻿using System;
using System.Collections.Generic;

namespace Knapcode.ExplorePackages
{
    public static partial class KustoDDL
    {
        private static Dictionary<Type, string> _typeToDefaultTableName;
        private static Dictionary<Type, IReadOnlyList<string>> _typeToDDL;

        public static IReadOnlyDictionary<Type, string> TypeToDefaultTableName => _typeToDefaultTableName;
        public static IReadOnlyDictionary<Type, IReadOnlyList<string>> TypeToDDL => _typeToDDL;

        private static bool AddTypeToDefaultTableName(Type type, string tableName)
        {
            if (_typeToDefaultTableName == null)
            {
                _typeToDefaultTableName = new Dictionary<Type, string>();
            }

            _typeToDefaultTableName.Add(type, tableName);
            return true;
        }

        private static bool AddTypeToDDL(Type type, IReadOnlyList<string> ddl)
        {
            if (_typeToDDL == null)
            {
                _typeToDDL = new Dictionary<Type, IReadOnlyList<string>>();
            }

            _typeToDDL.Add(type, ddl);
            return true;
        }
    }
}
