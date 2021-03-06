﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pihrtsoft.Snippets.CodeGeneration.Commands;

namespace Pihrtsoft.Snippets.CodeGeneration
{
    public class SnippetGenerator
    {
        public SnippetGenerator(SnippetGeneratorSettings settings)
        {
            Settings = settings;
        }

        private LanguageDefinition Language
        {
            get { return Settings.Language; }
        }

        public SnippetGeneratorSettings Settings { get; }

        private static Command StaticCommand { get; } = new StaticCommand();
        private static Command InitializerCommand { get; } = new InitializerCommand();
        private static Command ParametersCommand { get; } = new ParametersCommand();
        private static Command ArgumentsCommand { get; } = new ArgumentsCommand();

        public void GenerateSnippets(string sourceDirectoryPath, string destinationDirectoryPath)
        {
            Snippet[] snippets = SnippetSerializer.Deserialize(sourceDirectoryPath, SearchOption.AllDirectories)
                .SelectMany(snippet => GenerateSnippets(snippet))
                .ToArray();

            IOUtility.SaveSnippets(snippets, destinationDirectoryPath);
        }

        private IEnumerable<Snippet> GenerateSnippets(Snippet snippet)
        {
            var jobs = new JobCollection();

            jobs.AddCommands(GetTypeCommands(snippet));

            if (snippet.HasTag(KnownTags.GenerateCollection))
                jobs.AddCommands(GetCollectionCommands(snippet));

            if (snippet.HasTag(KnownTags.GenerateImmutableCollection))
                jobs.AddCommands(GetImmutableCollectionCommands(snippet));

            jobs.AddCommands(GetAccessModifierCommands(snippet));

            if (snippet.HasTag(KnownTags.GenerateStaticModifier))
                jobs.AddCommand(StaticCommand);

            if (snippet.HasTag(KnownTags.GenerateInitializer))
                jobs.AddCommand(InitializerCommand);

            if (snippet.HasTag(KnownTags.GenerateParameters))
                jobs.AddCommand(ParametersCommand);

            if (snippet.HasTag(KnownTags.GenerateArguments))
                jobs.AddCommand(ArgumentsCommand);

            if (snippet.HasTag(KnownTags.GenerateUnchanged))
                jobs.Add(new Job());

            foreach (Job job in jobs)
            {
                var context = new LanguageExecutionContext((Snippet)snippet.Clone(), Language);

                job.Execute(context);

                if (!context.IsCanceled)
                {
                    foreach (Snippet snippet2 in context.Snippets)
                    {
                        PostProcess(snippet2);
                        yield return snippet2;
                    }
                }
            }
        }

        private IEnumerable<Command> GetTypeCommands(Snippet snippet)
        {
            bool flg = false;

            foreach (TypeDefinition type in Language
                .Types
                .Where(f => snippet.RequiresTypeGeneration(f.Name)))
            {
                yield return new TypeCommand(type);

                if (!flg)
                {
                    yield return new TypeCommand(null);
                    flg = true;
                }
            }
        }

        private IEnumerable<Command> GetCollectionCommands(Snippet snippet)
        {
            return Settings
                .Types
                .Where(f => f.Tags.Contains(KnownTags.Collection) && !f.Tags.Contains(KnownTags.Immutable))
                .Select(f => new CollectionTypeCommand(f));
        }

        private IEnumerable<Command> GetImmutableCollectionCommands(Snippet snippet)
        {
            return Settings
                .Types
                .Where(f => f.Tags.Contains(KnownTags.Immutable))
                .Select(f => new ImmutableCollectionTypeCommand(f));
        }

        private IEnumerable<Command> GetAccessModifierCommands(Snippet snippet)
        {
            return Language
                .Modifiers
                .Where(modifier => modifier.Tags.Contains(KnownTags.AccessModifier) && snippet.RequiresModifierGeneration(modifier.Name))
                .Select(modifier => new AccessModifierCommand(modifier));
        }

        private void PostProcess(Snippet snippet)
        {
            ReplacePlaceholders(snippet);

            if (snippet.Language == Snippets.Language.VisualBasic)
                snippet.ReplaceSubOrFunctionLiteral("Function");

            Literal typeLiteral = snippet.Literals[LiteralIdentifiers.Type];

            if (typeLiteral != null)
                typeLiteral.DefaultValue = "T";

            RemoveUnusedLiterals(snippet);

            RemoveKeywords(snippet);

            snippet.AddTag(KnownTags.AutoGenerated);

            snippet.SortCollections();

            snippet.Author = "Josef Pihrt";

            if (snippet.SnippetTypes == SnippetTypes.None)
                snippet.SnippetTypes = SnippetTypes.Expansion;
        }

        private void ReplacePlaceholders(Snippet snippet)
        {
            snippet.Title = snippet.Title
                .ReplacePlaceholder(Placeholders.Type, " ", true)
                .ReplacePlaceholder(Placeholders.OfType, " ", true)
                .ReplacePlaceholder(Placeholders.GenericType, Language.GetTypeParameterList("T"));

            snippet.Description = snippet.Description
                .ReplacePlaceholder(Placeholders.Type, " ", true)
                .ReplacePlaceholder(Placeholders.OfType, " ", true)
                .ReplacePlaceholder(Placeholders.GenericType, Language.GetTypeParameterList("T"));
        }

        private void RemoveKeywords(Snippet snippet)
        {
            snippet.RemoveTags(Language.Types.Select(f => KnownTags.GenerateTypeTag(f.Name)));
            snippet.RemoveTags(Language.Modifiers.Select(f => KnownTags.GenerateModifierTag(f.Name)));

            snippet.RemoveTags(
                KnownTags.GenerateType,
                KnownTags.GenerateAccessModifier,
                KnownTags.GenerateInitializer,
                KnownTags.GenerateUnchanged,
                KnownTags.GenerateParameters,
                KnownTags.GenerateArguments,
                KnownTags.GenerateCollection,
                KnownTags.GenerateImmutableCollection,
                KnownTags.Array,
                KnownTags.Collection,
                KnownTags.Dictionary,
                KnownTags.TryParse,
                KnownTags.Initializer);
        }

        private static void RemoveUnusedLiterals(Snippet snippet)
        {
            for (int i = snippet.Literals.Count - 1; i >= 0; i--)
            {
                Literal literal = snippet.Literals[i];

                if (!literal.IsEditable
                    && string.IsNullOrEmpty(literal.DefaultValue))
                {
                    snippet.RemoveLiteralAndPlaceholders(literal);
                }
            }
        }
    }
}
