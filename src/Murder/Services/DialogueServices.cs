﻿using Bang;
using Bang.Entities;
using Murder.Assets;
using Murder.Components;
using Murder.Core.Dialogs;
using Murder.Diagnostics;

namespace Murder.Services
{
    public static class DialogueServices
    {
        public static CharacterRuntime? CreateCharacterFrom(Guid character, int situation)
        {
            Character? result = MurderSaveServices.CreateOrGetSave().BlackboardTracker
                .FetchCharacterFor(character);

            if (result is null)
            {
                return null;
            }

            return new CharacterRuntime(result.Value, situation);
        }

        public static LineComponent CreateLine(Line line)
        {
            return new(line, Game.NowUnscaled);
        }

        public static Line[] FetchAllLines(World world, Entity target, SituationComponent situation)
        {
            CharacterRuntime? character = CreateCharacterFrom(situation.Character, situation.Situation);
            if (character is null)
            {
                return Array.Empty<Line>();
            }

            List<Line>? lines = null;

            while (character.NextLine(world, target) is DialogLine dialogLine)
            {
                if (dialogLine.Line is Line line)
                {
                    lines ??= [];
                    lines.Add(line);
                }
                else if (dialogLine.Choice is ChoiceLine)
                {
                    GameLogger.Warning("We did not implement choices for fetching all lines yet.");
                    break;
                }
            }

            return lines?.ToArray() ?? Array.Empty<Line>();
        }

        public static string FetchFirstLine(World world, Entity? target, SituationComponent situation)
        {
            CharacterRuntime? character = CreateCharacterFrom(situation.Character, situation.Situation);
            if (character is null)
            {
                return string.Empty;
            }

            while (character.NextLine(world, target) is DialogLine dialogLine)
            {
                if (dialogLine.Line is Line line && line.IsText && line.Text is LocalizedString localizedString)
                {
                    return LocalizationServices.GetLocalizedString(localizedString);
                }
                else if (dialogLine.Choice is ChoiceLine)
                {
                    break;
                }
            }

            return string.Empty;
        }

        public static bool HasNewDialogue(World world, Entity? e, SituationComponent situation)
        {
            CharacterRuntime? character = CreateCharacterFrom(situation.Character, situation.Situation);
            if (character is null)
            {
                return false;
            }

            if (character.HasNewContentOnNextDialogueLine(world, e))
            {
                return true;
            }

            return false;
        }

        public static bool HasDialogue(World world, Entity? e, SituationComponent situation)
        {
            CharacterRuntime? character = CreateCharacterFrom(situation.Character, situation.Situation);
            if (character is null)
            {
                return false;
            }

            return character.HasNext(world, e);
        }
    }
}