﻿using System;
using System.Collections.Generic;
using System.Linq;
using Pihrtsoft.Records;

namespace Pihrtsoft.Snippets.CodeGeneration
{
    public static class LanguageDefinitionMapper
    {
        public static IEnumerable<LanguageDefinition> ToLanguageDefinitions(this IEnumerable<Record> records)
        {
            IEnumerable<IGrouping<string, Record>> x = records
                .GroupBy(f => f.GetStringOrDefault(Identifiers.Language));

            foreach (IGrouping<string, Record> grouping in records
                .GroupBy(f => f.GetStringOrDefault(Identifiers.Language)))
            {
                LanguageDefinition language = CreateLanguageDefinition((Language)Enum.Parse(typeof(Language), grouping.Key));

                foreach (ModifierDefinition modifier in grouping.ToModifierDefinitions())
                    language.Modifiers.Add(modifier);

                foreach (TypeDefinition type in grouping.ToTypeDefinitions())
                    language.Types.Add(type);

                yield return language;
            }
        }

        private static LanguageDefinition CreateLanguageDefinition(Language language)
        {
            switch (language)
            {
                case Language.CSharp:
                    return new CSharpDefinition();
                case Language.VisualBasic:
                    return new VisualBasicDefinition();
                default:
                    throw new NotSupportedException();
            }
        }

        public static IEnumerable<ModifierDefinition> ToModifierDefinitions(this IEnumerable<Record> records)
        {
            foreach (Record record in records.Where(f => f.Entity.Name == Identifiers.Modifier))
            {
                yield return new ModifierDefinition(
                    record.Id,
                    record.GetStringOrDefault(Identifiers.Keyword),
                    record.GetStringOrDefault(Identifiers.Shortcut),
                    record.Tags.ToArray());
            }
        }

        public static IEnumerable<TypeDefinition> ToTypeDefinitions(this IEnumerable<Record> records)
        {
            foreach (Record record in records.Where(f => f.Entity.Name == Identifiers.Type))
            {
                yield return new TypeDefinition(
                    record.Id,
                    record.GetStringOrDefault(Identifiers.Title, record.Id),
                    record.GetStringOrDefault(Identifiers.Keyword),
                    record.GetStringOrDefault(Identifiers.Shortcut),
                    record.GetStringOrDefault(Identifiers.DefaultValue),
                    record.GetStringOrDefault(Identifiers.DefaultIdentifier),
                    record.GetStringOrDefault(Identifiers.Namespace),
                    record.Tags.ToArray());
            }
        }

        private static class Identifiers
        {
            public const string Modifier = nameof(Modifier);
            public const string Language = nameof(Language);
            public const string Keyword = nameof(Keyword);
            public const string Shortcut = nameof(Shortcut);
            public const string DefaultValue = nameof(DefaultValue);
            public const string DefaultIdentifier = nameof(DefaultIdentifier);
            public const string Namespace = nameof(Namespace);
            public const string Type = nameof(Type);
            public const string Title = nameof(Title);
        }
    }
}
