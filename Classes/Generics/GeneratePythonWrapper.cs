using System;
using System.Reflection;
using System.Text;

namespace MarkGeometriesLib.Classes.Generics
{
    public static class GeneratePythonWrapper
    {
        private static readonly string Indentation = "    ";

        public static bool WritePythonWrapperToFile(string outputDir, string name, object classRef)
        {
            var filePath = Path.Combine(outputDir, $"{name}API.py");
            var text = GeneratePythonWrapperFromClassType($"{name}API", classRef.GetType()).ToString();
            File.WriteAllText(filePath, text);
            return File.Exists(filePath);
        }

        public static StringBuilder GeneratePythonWrapperFromClassType(string name, Type classTypeIn)
        {
            var sb = new StringBuilder();

            // add python libraries and imports
            AddHeader(sb);

            // add python class, constructor and public properties
            var localRefName = AddConstructor(sb, classTypeIn, name);

            // add python methods
            AddMethods(sb, localRefName, classTypeIn);
            return sb;
        }

        private static void AddHeader(StringBuilder sb)
        {
            sb.AppendLine("from __future__ import (");
            sb.AppendLine($"{Indentation}print_function,");
            sb.AppendLine($"{Indentation}division");
            sb.AppendLine(")");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("import os");
            sb.AppendLine("import sys");
            sb.AppendLine("import math");
            sb.AppendLine("import time");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("__here = os.path.dirname(os.path.realpath(__file__))");
            sb.AppendLine("sys.path.insert(0, __here)");
        }

        private static string AddConstructor(StringBuilder sb, Type classTypeInfo, string name)
        {
            // use invariant for non language specific data
            string localRefName = name.ToLowerInvariant();

            // PEP8 requires two line spacings
            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine($"class {name}(object):");
            sb.AppendLine($"{Indentation}def __init__(self, {localRefName} = None):");
            sb.AppendLine($"{Indentation}{Indentation}self.__{localRefName} = {localRefName}");

            // PEP8 requires two line spacings
            sb.AppendLine();
            sb.AppendLine();

            bool hasProperties = false;
            // add public class properties
            foreach (var property in classTypeInfo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                hasProperties = true;
                sb.AppendLine($"{Indentation}{Indentation}self.{property.Name} = None if self.__{localRefName} == None else self.__{localRefName}.{property.Name}");
            }

            if (hasProperties)
            {
                // PEP8 requires two line spacings
                sb.AppendLine();
                sb.AppendLine();
            }

            return localRefName;
        }

        private static void AddMethods(StringBuilder sb, string localRefName, Type classTypeInfo)
        {
            foreach (var method in classTypeInfo.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var methodParams = ToMethodInlineParams(method);
                sb.AppendLine($"{Indentation}def {method.Name}(self{methodParams.In}):");
                sb.AppendLine($"{Indentation}{Indentation}return self.__{localRefName}.{method.Name}({methodParams.Out})");

                // PEP8 requires two line spacings
                sb.AppendLine();
                sb.AppendLine();
            }
        }

        private static (string In, string Out) ToMethodInlineParams(MethodInfo methodInfo)
        {
            StringBuilder sbIn = new StringBuilder();
            StringBuilder sbOut = new StringBuilder();

            foreach (var param in methodInfo.GetParameters())
            {
                sbOut.Append($", {param.Name}");
                var defaultValue = $"{param.DefaultValue}";

                if (param.DefaultValue is string || param.DefaultValue is char)
                    sbIn.Append($", {param.Name} = '{param.DefaultValue}'");
                else if (string.IsNullOrEmpty(defaultValue))
                    sbIn.Append($", {param.Name}");
                else
                    sbIn.Append($", {param.Name} = {param.DefaultValue}");
            }

            return (sbIn.ToString(), sbOut.ToString().Trim(',', ' '));
        }
    }
}
