using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex;
using RCS.Profile;

namespace RCS.Profile.Test
{
    [TestClass]
    public class RcsStaticEngineProfilerTests
    {
        public TestContext TestContext { get; set; } = default!;

        [TestMethod]
        public void Profiler_12Thrusters_ProfilesEngineSuccessfully()
        {
            // Arrange
            var thrusters = CreateThrusters();
            var engine = new RcsEngine(thrusters);
            var profiler = new RcsStaticEngineProfiler<LexicographicGoalSolver>();

            // Act
            var profileResults = profiler.Profile(engine).ToList();

            // Assert
            Assert.IsTrue(profileResults.Count > 0, "Profile should produce results");
            Assert.IsTrue(profileResults.Last().Done, "Profile should complete");
            Assert.IsNotNull(profileResults.Last().Result, "Final result should not be null");

            TestContext.WriteLine($"12 Thrusters: Completed with {profileResults.Count} progress steps");
        }

        [TestMethod]
        public void Profiler_3ThrustersForce_ProfilesEngineSuccessfully()
        {
            // Arrange
            var thrusters = CreateThrusters3Fx();
            var engine = new RcsEngine(thrusters);
            var profiler = new RcsStaticEngineProfiler<LexicographicGoalSolver>();

            // Act
            var profileResults = profiler.Profile(engine).ToList();

            // Assert
            Assert.IsTrue(profileResults.Count > 0, "Profile should produce results");
            Assert.IsTrue(profileResults.Last().Done, "Profile should complete");
            Assert.IsNotNull(profileResults.Last().Result, "Final result should not be null");

            TestContext.WriteLine($"3 Thrusters (Force): Completed with {profileResults.Count} progress steps");
        }

        [TestMethod]
        public void Profiler_4ThrustersForce_ProfilesEngineSuccessfully()
        {
            // Arrange
            var thrusters = CreateThrusters4Fx();
            var engine = new RcsEngine(thrusters);
            var profiler = new RcsStaticEngineProfiler<LexicographicGoalSolver>();

            // Act
            var profileResults = profiler.Profile(engine).ToList();

            // Assert
            Assert.IsTrue(profileResults.Count > 0, "Profile should produce results");
            Assert.IsTrue(profileResults.Last().Done, "Profile should complete");
            Assert.IsNotNull(profileResults.Last().Result, "Final result should not be null");

            TestContext.WriteLine($"4 Thrusters (Force): Completed with {profileResults.Count} progress steps");
        }

        [TestMethod]
        public void Profiler_3ThrustersOpposite_ProfilesEngineSuccessfully()
        {
            // Arrange
            var thrusters = CreateThrusters3opposite();
            var engine = new RcsEngine(thrusters);
            var profiler = new RcsStaticEngineProfiler<LexicographicGoalSolver>();

            // Act
            var profileResults = profiler.Profile(engine).ToList();

            // Assert
            Assert.IsTrue(profileResults.Count > 0, "Profile should produce results");
            Assert.IsTrue(profileResults.Last().Done, "Profile should complete");
            Assert.IsNotNull(profileResults.Last().Result, "Final result should not be null");

            TestContext.WriteLine($"3 Thrusters (Opposite): Completed with {profileResults.Count} progress steps");
        }

        [TestMethod]
        public void Profiler_Random12Thrusters_ProfilesEngineSuccessfully()
        {
            // Arrange
            var thrusters = CreateThrustersRandom2Fx();
            var engine = new RcsEngine(thrusters);
            var profiler = new RcsStaticEngineProfiler<LexicographicGoalSolver>();

            // Act
            var profileResults = profiler.Profile(engine).ToList();

            // Assert
            Assert.IsTrue(profileResults.Count > 0, "Profile should produce results");
            Assert.IsTrue(profileResults.Last().Done, "Profile should complete");
            Assert.IsNotNull(profileResults.Last().Result, "Final result should not be null");

            TestContext.WriteLine($"Random 12 Thrusters: Completed with {profileResults.Count} progress steps");
        }

        [TestMethod]
        public void Profiler_UnityThrusters_ProfilesEngineSuccessfully()
        {
            // Arrange
            var thrusters = CreateThrustersUnity();
            var engine = new RcsEngine(thrusters);
            var profiler = new RcsStaticEngineProfiler<LexicographicGoalSolver>();

            // Act
            var profileResults = profiler.Profile(engine).ToList();

            // Assert
            Assert.IsTrue(profileResults.Count > 0, "Profile should produce results");
            Assert.IsTrue(profileResults.Last().Done, "Profile should complete");
            Assert.IsNotNull(profileResults.Last().Result, "Final result should not be null");

            TestContext.WriteLine($"Unity Thrusters: Completed with {profileResults.Count} progress steps");
        }

        // Helper methods copied from ThrusterTestData
        private static Dictionary<string, RcsThruster> CreateThrusters()
        {
            return new Dictionary<string, RcsThruster>
            {
                ["T1"] = new RcsThruster(new RcsVector<int>(1, 0, 0), new RcsVector<int>(2, 1, 1)),
                ["T2"] = new RcsThruster(new RcsVector<int>(1, 0, 0), new RcsVector<int>(2, -1, -1)),
                ["T3"] = new RcsThruster(new RcsVector<int>(-1, 0, 0), new RcsVector<int>(-2, 1, -1)),
                ["T4"] = new RcsThruster(new RcsVector<int>(-1, 0, 0), new RcsVector<int>(-2, -1, 1)),
                ["T5"] = new RcsThruster(new RcsVector<int>(0, 1, 0), new RcsVector<int>(1, 2, -1)),
                ["T6"] = new RcsThruster(new RcsVector<int>(0, 1, 0), new RcsVector<int>(-1, 2, 1)),
                ["T7"] = new RcsThruster(new RcsVector<int>(0, -1, 0), new RcsVector<int>(1, -2, 1)),
                ["T8"] = new RcsThruster(new RcsVector<int>(0, -1, 0), new RcsVector<int>(-1, -2, -1)),
                ["T9"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(1, -1, 2)),
                ["T10"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(-1, 1, 2)),
                ["T11"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(1, 1, -2)),
                ["T12"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(-1, -1, -2)),
            };
        }

        private static Dictionary<string, RcsThruster> CreateThrusters3Fx()
        {
            return new Dictionary<string, RcsThruster>
            {
                ["T1"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(1, -1, 0)),
                ["T2"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(-1, -1, 0)),
                ["T3"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(0, 1, 0)),
            };
        }

        private static Dictionary<string, RcsThruster> CreateThrusters4Fx()
        {
            return new Dictionary<string, RcsThruster>
            {
                ["T1"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(1, 1, 0)),
                ["T2"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(1, -1, 0)),
                ["T3"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(-1, 1, 0)),
                ["T4"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(-1, -1, 0)),
            };
        }

        private static Dictionary<string, RcsThruster> CreateThrusters3opposite()
        {
            return new Dictionary<string, RcsThruster>
            {
                ["T1"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(1, 1, 1)),
                ["T2"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(1, -1, 1)),
                ["T3"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(0, 1, 1)),

                ["T4"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(1, 1, -1)),
                ["T5"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(1, -1, -1)),
                ["T6"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(0, -1, -1)),
            };
        }

        private static Dictionary<string, RcsThruster> CreateThrustersRandom2Fx()
        {
            return new Dictionary<string, RcsThruster>
            {
                ["R1"] = new RcsThruster(new RcsVector<int>(1, 0, 0), new RcsVector<int>(6, 4, -3)),
                ["R2"] = new RcsThruster(new RcsVector<int>(1, 0, 0), new RcsVector<int>(-8, -2, 1)),
                ["R3"] = new RcsThruster(new RcsVector<int>(-1, 0, 0), new RcsVector<int>(-4, 7, -6)),
                ["R4"] = new RcsThruster(new RcsVector<int>(-1, 0, 0), new RcsVector<int>(9, -5, 2)),

                ["R5"] = new RcsThruster(new RcsVector<int>(0, 1, 0), new RcsVector<int>(3, 8, -4)),
                ["R6"] = new RcsThruster(new RcsVector<int>(0, 1, 0), new RcsVector<int>(-2, 11, 5)),
                ["R7"] = new RcsThruster(new RcsVector<int>(0, -1, 0), new RcsVector<int>(1, -10, 7)),
                ["R8"] = new RcsThruster(new RcsVector<int>(0, -1, 0), new RcsVector<int>(-7, -9, -2)),

                ["R9"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(5, -3, 12)),
                ["R10"] = new RcsThruster(new RcsVector<int>(0, 0, 1), new RcsVector<int>(-6, 6, 9)),
                ["R11"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(4, 5, -11)),
                ["R12"] = new RcsThruster(new RcsVector<int>(0, 0, -1), new RcsVector<int>(-5, -4, -8)),
            };
        }

        private static Dictionary<string, RcsThruster> CreateThrustersUnity()
        {
            return new Dictionary<string, RcsThruster>
            {
                ["T1"] = new RcsThruster(new RcsVector<int>(0, 0, -10), new RcsVector<int>(-1, -1, 0)),
                ["T2"] = new RcsThruster(new RcsVector<int>(0, 0, -10), new RcsVector<int>(1, -1, 0)),
                ["T3"] = new RcsThruster(new RcsVector<int>(0, 0, -10), new RcsVector<int>(-1, 1, 0)),
                ["T4"] = new RcsThruster(new RcsVector<int>(0, 0, -10), new RcsVector<int>(1, 1, 0))
            };
        }
    }
}
