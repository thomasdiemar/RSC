using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex;
using RCS.Profile;
using ThrusterOptimizationTests;

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
            var thrusters = ThrusterTestData.CreateThrusters();
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
            var thrusters = ThrusterTestData.CreateThrusters3Fx();
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
            var thrusters = ThrusterTestData.CreateThrusters4Fx();
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
            var thrusters = ThrusterTestData.CreateThrusters3opposite();
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
            var thrusters = ThrusterTestData.CreateThrustersRandom2Fx();
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
            var thrusters = ThrusterTestData.CreateThrustersUnity();
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
    }
}
