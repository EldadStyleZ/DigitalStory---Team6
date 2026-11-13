using System.Collections.Generic;
using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class ControlPromptTests
    {
        private static readonly string[] HoleNames = { "H", "J", "K", "L" };

        private static readonly (string Keys, string Pad)[] Swaps =
        {
            ("W A S D", "the left stick"),
            ("E", "RB"),
            ("Enter", "Start"),
            ("R", "Start"),
        };

        private static List<PromptPart> Split(string text)
        {
            var parts = new List<PromptPart>();
            ControlPrompt.Split(text, HoleNames, Swaps, parts);
            return parts;
        }

        private static void ExpectText(PromptPart part, string text)
        {
            Assert.That(part.IsHole, Is.False, "expected the text '" + text + "'");
            Assert.That(part.Text, Is.EqualTo(text));
        }

        private static void ExpectHole(PromptPart part, int hole)
        {
            Assert.That(part.IsHole, Is.True, "expected hole " + hole + ", got the text '" + part.Text + "'");
            Assert.That(part.Hole, Is.EqualTo(hole));
        }

        [TestCase("")]
        [TestCase(null)]
        public void Split_NoText_HasNoPart(string text)
        {
            Assert.That(Split(text), Is.Empty);
        }

        [Test]
        public void Split_TextWithoutKeys_IsOneTextPart()
        {
            List<PromptPart> parts = Split("Fly to a hive to take its wasps.");

            Assert.That(parts.Count, Is.EqualTo(1));
            ExpectText(parts[0], "Fly to a hive to take its wasps.");
        }

        [Test]
        public void Split_NotesWithAFullStop_AreHolesThenTheStop()
        {
            List<PromptPart> parts = Split("Play J K H.");

            Assert.That(parts.Count, Is.EqualTo(5));
            ExpectText(parts[0], "Play");
            ExpectHole(parts[1], 1);
            ExpectHole(parts[2], 2);
            ExpectHole(parts[3], 0);
            ExpectText(parts[4], ".");
        }

        [Test]
        public void Split_GiantMoves_KeepTheWordsBetweenTheNotes()
        {
            List<PromptPart> parts = Split("J punch · K kick · L slam · H roar");

            Assert.That(parts.Count, Is.EqualTo(8));
            ExpectHole(parts[0], 1);
            ExpectText(parts[1], "punch ·");
            ExpectHole(parts[2], 2);
            ExpectText(parts[3], "kick ·");
            ExpectHole(parts[4], 3);
            ExpectText(parts[5], "slam ·");
            ExpectHole(parts[6], 0);
            ExpectText(parts[7], "roar");
        }

        [Test]
        public void Split_HumLine_KeepsTheColonAndTheDots()
        {
            List<PromptPart> parts = Split("A nest hums: J K ...");

            Assert.That(parts.Count, Is.EqualTo(4));
            ExpectText(parts[0], "A nest hums:");
            ExpectHole(parts[1], 1);
            ExpectHole(parts[2], 2);
            ExpectText(parts[3], "...");
        }

        [TestCase("Hold E to heal", "Hold RB to heal")]
        [TestCase("Enter: call the wave early", "Start: call the wave early")]
        [TestCase("Enemies come when the timer ends. Press Enter when ready.", "Enemies come when the timer ends. Press Start when ready.")]
        [TestCase("Press R to play again", "Press Start to play again")]
        [TestCase("Move with W A S D.", "Move with the left stick.")]
        public void Split_KeyWords_AreSwappedForThePads(string text, string expected)
        {
            List<PromptPart> parts = Split(text);

            Assert.That(parts.Count, Is.EqualTo(1));
            ExpectText(parts[0], expected);
        }

        // Only a whole word in the same case is a key: Each, Hold, a and Lv are words.
        [Test]
        public void Split_KeyLettersInsideWords_StayWords()
        {
            List<PromptPart> parts = Split("Each Hold a Lv Rest Ever");

            Assert.That(parts.Count, Is.EqualTo(1));
            ExpectText(parts[0], "Each Hold a Lv Rest Ever");
        }

        [Test]
        public void Split_PartOfAPhrase_IsNotSwapped()
        {
            List<PromptPart> parts = Split("W A then");

            Assert.That(parts.Count, Is.EqualTo(1));
            ExpectText(parts[0], "W A then");
        }

        [Test]
        public void Split_CalledAgain_StartsAfresh()
        {
            var parts = new List<PromptPart>();
            ControlPrompt.Split("Play J K H.", HoleNames, Swaps, parts);

            ControlPrompt.Split("Hold E to heal", HoleNames, Swaps, parts);

            Assert.That(parts.Count, Is.EqualTo(1));
        }
    }
}
