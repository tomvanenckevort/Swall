using System;
using System.Collections.Generic;
using System.Text;
using VYaml.Parser;

namespace Swall.Yaml
{
    internal static class YamlDeserializer
    {
        /// <summary>
        /// Deserializes YAML string and returns a dictionary containing the YAML keys and values.
        /// </summary>
        /// <param name="yaml"></param>
        /// <returns></returns>
        public static Dictionary<string, object> Deserialize(string yaml)
        {
            var dictionary = new Dictionary<string, object>();

            var yamlBytes = new Memory<byte>(Encoding.UTF8.GetBytes(yaml));

            var parser = YamlParser.FromBytes(yamlBytes);

            parser.SkipAfter(ParseEventType.DocumentStart);

            parser.Read();

            while (!parser.End)
            {
                var key = ConvertToObject(ref parser);

                var value = ConvertToObject(ref parser);

                if (key is string keyStr)
                {
                    dictionary.Add(keyStr, value);
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Converts YAML node values to the appropriate object.
        /// </summary>
        /// <param name="parser"></param>
        /// <returns></returns>
        private static object ConvertToObject(ref YamlParser parser)
        {
            if (parser.CurrentEventType == ParseEventType.MappingStart)
            {
                var values = new Dictionary<string, object>();

                parser.SkipAfter(ParseEventType.MappingStart);

                while (!parser.End && parser.CurrentEventType != ParseEventType.MappingEnd)
                {
                    var key = ConvertToObject(ref parser);

                    var value = ConvertToObject(ref parser);

                    if (key is string keyStr)
                    {
                        values.Add(keyStr, value);
                    }
                }

                parser.SkipAfter(ParseEventType.MappingEnd);

                return values;
            }
            else if (parser.CurrentEventType == ParseEventType.Scalar)
            {
                object scalarResult;

                if (parser.TryGetScalarAsBool(out var scalarBool))
                {
                    scalarResult = scalarBool;
                }
                else if (parser.TryGetScalarAsDouble(out var scalarDouble))
                {
                    scalarResult = scalarDouble;
                }
                else if (parser.TryGetScalarAsFloat(out var scalarFloat))
                {
                    scalarResult = scalarFloat;
                }
                else if (parser.TryGetScalarAsInt32(out var scalarInt32))
                {
                    scalarResult = scalarInt32;
                }
                else if (parser.TryGetScalarAsInt64(out var scalarInt64))
                {
                    scalarResult = scalarInt64;
                }
                else if (parser.TryGetScalarAsString(out var scalarString))
                {
                    scalarResult = scalarString;
                }
                else
                {
                    scalarResult = null;
                }

                parser.Read();

                return scalarResult;
            }
            else if (parser.CurrentEventType == ParseEventType.SequenceStart)
            {
                var values = new List<object>();

                parser.SkipAfter(ParseEventType.SequenceStart);

                while (!parser.End && parser.CurrentEventType != ParseEventType.SequenceEnd)
                {
                    values.Add(ConvertToObject(ref parser));
                }

                parser.SkipAfter(ParseEventType.SequenceEnd);

                return values.ToArray();
            }
            else
            {
                parser.Read();

                return null;
            }
        }
    }
}
