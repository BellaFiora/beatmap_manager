// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Graphics.Containers;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Screens;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Play;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tests.Visual
{
    public class TestCaseFacadeContainer : ScreenTestCase
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(PlayerLoader),
            typeof(Player),
            typeof(Facade),
        };

        [Cached]
        private OsuLogo logo;

        private readonly Bindable<float> uiScale = new Bindable<float>();

        private TestScreen screen1;
        private OsuScreen baseScreen;

        public TestCaseFacadeContainer()
        {
            Add(logo = new OsuLogo());
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            baseScreen = null;
            config.BindWith(OsuSetting.UIScale, uiScale);
            AddSliderStep("Adjust scale", 1f, 1.5f, 1f, v => uiScale.Value = v);
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("Null screens", () => baseScreen = null);
        }

        [Test]
        public void IsolatedTest()
        {
            bool randomPositions = false;
            AddToggleStep("Toggle move continuously", b => randomPositions = b);
            AddStep("Move facade to random position", () => LoadScreen(screen1 = new TestScreen(randomPositions)));
        }

        [Test]
        public void PlayerLoaderTest()
        {
            AddToggleStep("Toggle mods", b => { Beatmap.Value.Mods.Value = b ? Beatmap.Value.Mods.Value.Concat(new[] { new OsuModNoFail() }) : Enumerable.Empty<Mod>(); });
            AddStep("Add new playerloader", () => LoadScreen(baseScreen = new TestPlayerLoader(() => new TestPlayer
            {
                AllowPause = false,
                AllowLeadIn = false,
                AllowResults = false,
            })));
        }

        [Test]
        public void MainMenuTest()
        {
            AddStep("Add new Main Menu", () => LoadScreen(baseScreen = new MainMenu()));
        }

        private class TestFacadeContainer : FacadeContainer
        {
            protected override Facade CreateFacade() => new Facade
            {
                Colour = Color4.Tomato,
                Alpha = 0.35f,
                Child = new Box
                {
                    Colour = Color4.Tomato,
                    RelativeSizeAxes = Axes.Both,
                },
            };
        }

        private class TestScreen : OsuScreen
        {
            private TestFacadeContainer facadeContainer;
            private FacadeFlowComponent facadeFlowComponent;
            private OsuLogo logo;

            private readonly bool randomPositions;

            public TestScreen(bool randomPositions = false)
            {
                this.randomPositions = randomPositions;
            }

            private SpriteText positionText;
            private SpriteText sizeAxesText;

            [BackgroundDependencyLoader]
            private void load()
            {
                InternalChild = facadeContainer = new TestFacadeContainer
                {
                    Child = facadeFlowComponent = new FacadeFlowComponent
                    {
                        AutoSizeAxes = Axes.Both
                    }
                };
            }

            protected override void LogoArriving(OsuLogo logo, bool resuming)
            {
                base.LogoArriving(logo, resuming);
                logo.FadeIn(350);
                logo.ScaleTo(new Vector2(0.15f), 350, Easing.In);
                facadeContainer.SetLogo(logo);
                moveLogoFacade();
            }

            private void moveLogoFacade()
            {
                Random random = new Random();
                if (facadeFlowComponent.Transforms.Count == 0)
                {
                    facadeFlowComponent.Delay(500).MoveTo(new Vector2(random.Next(0, 800), random.Next(0, 600)), 300);
                }

                if (randomPositions)
                    Schedule(moveLogoFacade);
            }
        }

        private class FacadeFlowComponent : FillFlowContainer
        {
            [BackgroundDependencyLoader]
            private void load(Facade facade)
            {
                facade.Anchor = Anchor.TopCentre;
                facade.Origin = Anchor.TopCentre;
                Child = facade;
            }
        }

        private class TestPlayerLoader : PlayerLoader
        {
            public TestPlayerLoader(Func<Player> player)
                : base(player)
            {
            }

            protected override FacadeContainer CreateFacadeContainer() => new TestFacadeContainer();
        }

        private class TestPlayer : Player
        {
            [BackgroundDependencyLoader]
            private void load()
            {
                // Never finish loading
                while (true)
                    Thread.Sleep(1);
            }
        }
    }
}
