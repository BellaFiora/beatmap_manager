// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.Bindings;
using System.Collections.Generic;
using osu.Framework.Platform;
using osu.Game.Tests;
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Threading;

namespace BellaFioraUtils
{
    public class BellaFioraHost : CleanRunHeadlessGameHost
    {
        public override bool CanExit => false;
        public override IEnumerable<KeyBinding> PlatformKeyBindings => new List<KeyBinding>();
        public BellaFioraHost()
        : base(false, false, false, false, "BellaFioraUtils")
        {
        }

        protected override IWindow? CreateWindow(GraphicsSurfaceType preferredSurface)
        {
            return null;
        }

        // Taken from osu.Game.Tests.ImportTest.cs
        private void waitForOrAssert(Func<bool> result, string failureMessage, int timeout = 60000)
        {
            Task task = Task.Run(() =>
            {
                while (!result()) Thread.Sleep(200);
            });

            Assert.IsTrue(task.Wait(timeout), failureMessage);
        }

        // Taken from osu.Game.Tests.ImportTest.cs
        public void Start()
        {
            Task.Factory.StartNew(() => Run(Utils.Osu), TaskCreationOptions.LongRunning)
                .ContinueWith(t => Assert.Fail($"Host threw exception {t.Exception}"), TaskContinuationOptions.OnlyOnFaulted);

            waitForOrAssert(() => Utils.Osu.IsLoaded, @"osu! failed to start in a reasonable amount of time");

            bool ready = false;
            // wait for two update frames to be executed. this ensures that all components have had a change to run LoadComplete and hopefully avoid
            // database access (GlobalActionContainer is one to do this).
            UpdateThread.Scheduler.Add(() => UpdateThread.Scheduler.Add(() => ready = true));

            waitForOrAssert(() => ready, @"osu! failed to start in a reasonable amount of time");
        }
    }
}
