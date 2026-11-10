using NUnit.Framework;

namespace SpacePartyGame.Tests
{
    public sealed class MenuStackTests
    {
        [Test]
        public void Top_Empty_IsNone()
        {
            var stack = new MenuStack();

            Assert.That(stack.Top, Is.EqualTo(MenuId.None));
            Assert.That(stack.IsAnyOpen, Is.False);
        }

        [Test]
        public void Push_OverAnother_OnlyTheNewOneIsShown()
        {
            var stack = new MenuStack();
            stack.Push(MenuId.Pause);

            stack.Push(MenuId.Settings);

            Assert.That(stack.IsShown(MenuId.Settings), Is.True);
            Assert.That(stack.IsShown(MenuId.Pause), Is.False);
            Assert.That(stack.IsAnyOpen, Is.True);
        }

        [Test]
        public void Pop_ShowsTheMenuUnderneathAgain()
        {
            var stack = new MenuStack();
            stack.Push(MenuId.Pause);
            stack.Push(MenuId.Settings);

            stack.Pop();

            Assert.That(stack.Top, Is.EqualTo(MenuId.Pause));
        }

        [Test]
        public void Pop_Empty_StaysEmpty()
        {
            var stack = new MenuStack();

            stack.Pop();

            Assert.That(stack.IsAnyOpen, Is.False);
        }

        [Test]
        public void ReplaceWith_ClearsEverythingAndOpensOne()
        {
            var stack = new MenuStack();
            stack.Push(MenuId.Pause);
            stack.Push(MenuId.Settings);

            stack.ReplaceWith(MenuId.Leaderboard);

            Assert.That(stack.Top, Is.EqualTo(MenuId.Leaderboard));
            stack.Pop();
            Assert.That(stack.IsAnyOpen, Is.False);
        }

        [Test]
        public void ReplaceWith_None_ClosesEverything()
        {
            var stack = new MenuStack();
            stack.Push(MenuId.Join);

            stack.ReplaceWith(MenuId.None);

            Assert.That(stack.IsAnyOpen, Is.False);
        }

        [Test]
        public void Push_None_IsIgnored()
        {
            var stack = new MenuStack();

            stack.Push(MenuId.None);

            Assert.That(stack.IsAnyOpen, Is.False);
        }

        [Test]
        public void Changed_RaisedOnEveryChange()
        {
            var stack = new MenuStack();
            int changes = 0;
            stack.Changed += () => changes++;

            stack.Push(MenuId.Pause);
            stack.Push(MenuId.Settings);
            stack.Pop();
            stack.ReplaceWith(MenuId.None);

            Assert.That(changes, Is.EqualTo(4));
        }
    }

    public sealed class MenuRulesTests
    {
        [TestCase(GameState.StartMenu, MenuId.StartMenu)]
        [TestCase(GameState.Join, MenuId.Join)]
        [TestCase(GameState.Countdown, MenuId.None)]
        [TestCase(GameState.Playing, MenuId.None)]
        [TestCase(GameState.Paused, MenuId.Pause)]
        [TestCase(GameState.RoundEnd, MenuId.Winner)]
        [TestCase(GameState.NameEntry, MenuId.NameEntry)]
        [TestCase(GameState.Leaderboard, MenuId.Leaderboard)]
        public void MenuFor_EachState(GameState state, MenuId expected)
        {
            Assert.That(MenuRules.MenuFor(state), Is.EqualTo(expected));
        }
    }
}
