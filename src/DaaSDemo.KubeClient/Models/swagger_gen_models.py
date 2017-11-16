"""
Generate model classes from Kubernetes swagger.
"""

import yaml

ROOT_NAMESPACE = 'DaaSDemo.KubeClient.Models'
LINE_ENDING = '\n'

def capitalize_name(name):
    return name[0].capitalize() + name[1:]

def get_type_info(definition_name):
    name_components = definition_name.split('.')
    if len(name_components) < 2:
        return (definition_name[-1], '')

    type_name = capitalize_name(name_components[-1])

    api_version = name_components[-2].capitalize().replace(
        'alpha', 'Alpha'
    ).replace(
        'beta', 'Beta'
    )

    return (type_name, api_version)

def get_defname_sort_key(definition_name):
    (type_name, _) = get_type_info(definition_name)

    return type_name

def get_cts_type_name(swagger_type_name):
    if swagger_type_name == 'integer':
        return 'int'

    if swagger_type_name == 'boolean':
        return 'bool'

    return swagger_type_name

def get_property_type_info(property_definition, is_array=False, is_dict=False):
    if 'type' in property_definition:
        property_type = property_definition['type']
        if property_type == 'array':
            is_array = True

            return get_property_type_info(property_definition['items'], is_array=True)
        elif property_type == 'object':
            is_dict = True
            (property_type_name, property_api_version) = ('Dictionary<string, string>', '')
        else:
            (property_type_name, property_api_version) = (get_cts_type_name(property_type), '')
    else:
        type_ref = property_definition['$ref']
        if (type_ref == 'io.k8s.apimachinery.pkg.apis.meta.v1.Time'):
            (property_type_name, property_api_version) = ('DateTime', '')
        else:
            (property_type_name, property_api_version) = get_type_info(property_definition['$ref'].replace('#/definitions/', ''))

    return (property_type_name, property_api_version, is_array, is_dict)

def main():
    with open('kube-swagger.yml') as kube_swagger_file:
        kube_swagger = yaml.load(kube_swagger_file)

    definitions = kube_swagger["definitions"]
    for definition_name in sorted(definitions.keys(), key=get_defname_sort_key):
        if definition_name.startswith('apimachinery.pkg.'):
            continue

        definition = definitions[definition_name]
        if 'type' in definition:
            continue

        (type_name, api_version) = get_type_info(definition_name)

        if 'description' in definition:
            description = definition['description']
        else:
            description = 'No description provided.'

        with open(type_name + '.cs', 'w') as class_file:
            class_file.write('using Newtonsoft.Json;' + LINE_ENDING)
            class_file.write('using System;' + LINE_ENDING)
            class_file.write('using System.Collections.Generic;' + LINE_ENDING)
            class_file.write(LINE_ENDING)
            class_file.write('namespace ' + ROOT_NAMESPACE + LINE_ENDING)
            class_file.write('{' + LINE_ENDING)

            class_file.write('    /// <summary>' + LINE_ENDING)

            for description_line in description.split('\n'):
                class_file.write('    ///     ' + description_line + LINE_ENDING)
            class_file.write('    /// </summary>' + LINE_ENDING)

            class_file.write('    public class %s%s%s' % (type_name, api_version, LINE_ENDING))
            class_file.write('    {' + LINE_ENDING)

            if 'properties' in definition:
                properties = definition['properties']
                property_names = [name for name in properties.keys()]

                for property_index in range(0, len(property_names) - 1):
                    property_name = property_names[property_index]
                    property_definition = properties[property_name]

                    json_property_name = property_name
                    property_name = capitalize_name(property_name)

                    (property_type_name, property_api_version, is_array, _) = get_property_type_info(property_definition)
                    property_type_name += property_api_version

                    if is_array:
                        property_type_name = 'List<{}>'.format(property_type_name)

                    class_file.write('        /// <summary>' + LINE_ENDING)
                    if 'description' in property_definition:
                        property_description = property_definition['description']

                        for property_description_line in property_description.split('\n'):
                            class_file.write('        ///     ' + property_description_line + LINE_ENDING)
                    else:
                        class_file.write('        ///     No description provided.' + LINE_ENDING)
                    class_file.write('        /// </summary>' + LINE_ENDING)

                    class_file.write('        [JsonProperty("%s")]%s' % (json_property_name, LINE_ENDING))
                    class_file.write('        public %s %s { get; set; }%s' % (property_type_name, property_name, LINE_ENDING))

                    if property_index + 2 < len(property_names): # Hur, dur, MAGIC
                        class_file.write(LINE_ENDING)

            class_file.write('    }' + LINE_ENDING) # Class

            class_file.write('}' + LINE_ENDING) # Namespace
main()
