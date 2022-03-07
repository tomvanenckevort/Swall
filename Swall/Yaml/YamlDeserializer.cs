using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpYaml.Serialization;

namespace Swall.Yaml
{
    internal class YamlDeserializer
    {
        /// <summary>
        /// Deserializes YAML string and returns a dictionary containing the YAML keys and values.
        /// </summary>
        /// <param name="yaml"></param>
        /// <returns></returns>
        public Dictionary<string, object> Deserialize(string yaml)
        {
            var dictionary = new Dictionary<string, object>();

            using var stringReader = new StringReader(yaml);

            var stream = new YamlStream();
            stream.Load(stringReader);

            if (stream.Documents?.Count < 1)
            {
                return dictionary;
            }

            var document = stream.Documents[0];

            if (document.AllNodes.FirstOrDefault() is not YamlMappingNode mappingNode)
            {
                return dictionary;
            }

            dictionary = ConvertToObject(mappingNode) as Dictionary<string, object>;

            return dictionary;
        }

        /// <summary>
        /// Converts YAML node value to the appropriate object.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private object ConvertToObject(YamlNode node)
        {
            if (node is YamlMappingNode mappingNode)
            {
                var values = new Dictionary<string, object>(mappingNode.Children.Count);

                foreach (var pair in mappingNode.Children)
                {
                    var key = pair.Key.ToString();

                    var value = ConvertToObject(pair.Value);

                    values.Add(key, value);
                }

                return values;
            }
            else if (node is YamlScalarNode scalarNode)
            {
                return scalarNode.Value;
            }
            else if (node is YamlSequenceNode sequenceNode)
            {
                var values = new object[sequenceNode.Children.Count];

                var index = 0;

                foreach (var child in sequenceNode.Children)
                {
                    values[index] = ConvertToObject(child);

                    index++;
                }

                return values;
            }
            else
            {
                return null;
            }
        }
    }
}
