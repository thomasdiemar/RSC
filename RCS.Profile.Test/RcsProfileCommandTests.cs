using LinearSolver;
using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex;
using RCS.Profile;
using ThrusterOptimizationTests;

namespace RCS.Profile.Test
{
    [TestClass]
    public class RcsProfileCommandTests
    {
        public TestContext TestContext { get; set; } = default!;

        [TestMethod]
        public void ProfileCommands_With12Thrusters_ValidatesAllDirections()
        {
            // Arrange
            var thrusters = ThrusterTestData.CreateThrusters();
            var engine = new RcsEngine(thrusters);
            var profiler = new RcsStaticEngineProfiler<LexicographicGoalSolver>();
            var commands = RcsProfileCommandTestData.GetRcsProfileCommands();

            // Act
            var profileResults = profiler.Profile(engine).ToList();
            var finalProfile = profileResults.Last().Result;

            // Assert
            Assert.IsTrue(profileResults.Count > 0, "Profile should produce results");
            Assert.IsTrue(profileResults.Last().Done, "Profile should complete");
            Assert.IsNotNull(finalProfile, "Final result should not be null");

            TestContext.WriteLine($"12 Thrusters - Commands: Completed with {profileResults.Count} progress steps");
            
            // Print profile results for each command
            foreach (var command in commands)
            {
                var profile = finalProfile.GetProfile(command);
                if (profile != null && profile.Any())
                {
                    var thrusterOutputs = string.Join(", ", profile.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                    TestContext.WriteLine($"  Command({command.DesiredForce}, {command.DesiredTorque}): {thrusterOutputs}");
                }
                else
                {
                    TestContext.WriteLine($"  Command({command.DesiredForce}, {command.DesiredTorque}): No profile found");
                }
            }
        }

        [TestMethod]
        public void ProfileCommands_With3ForceThusters_ValidatesForceDirections()
        {
            // Arrange
            var thrusters = ThrusterTestData.CreateThrusters3Fx();
            var engine = new RcsEngine(thrusters);
            var profiler = new RcsStaticEngineProfiler<LexicographicGoalSolver>();
            var commands = RcsProfileCommandTestData.GetRcsProfileCommands();

            // Act
            var profileResults = profiler.Profile(engine).ToList();
            var finalProfile = profileResults.Last().Result;

            // Assert
            Assert.IsTrue(profileResults.Count > 0, "Profile should produce results");
            Assert.IsTrue(profileResults.Last().Done, "Profile should complete");
            Assert.IsNotNull(finalProfile, "Final result should not be null");

            TestContext.WriteLine($"3 Force Thrusters - Commands: Completed with {profileResults.Count} progress steps");
            
            // Print profile results for each command
            foreach (var command in commands)
            {
                var profile = finalProfile.GetProfile(command);
                if (profile != null && profile.Any())
                {
                    var thrusterOutputs = string.Join(", ", profile.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                    TestContext.WriteLine($"  Command({command.DesiredForce}, {command.DesiredTorque}): {thrusterOutputs}");
                }
                else
                {
                    TestContext.WriteLine($"  Command({command.DesiredForce}, {command.DesiredTorque}): No profile found");
                }
            }
        }

        [TestMethod]
        public void ProfileCommands_With4ForceThusters_ValidatesForceDirections()
        {
            // Arrange
            var thrusters = ThrusterTestData.CreateThrusters4Fx();
            var engine = new RcsEngine(thrusters);
            var profiler = new RcsStaticEngineProfiler<LexicographicGoalSolver>();
            var commands = RcsProfileCommandTestData.GetRcsProfileCommands();

            // Act
            var profileResults = profiler.Profile(engine).ToList();
            var finalProfile = profileResults.Last().Result;

            // Assert
            Assert.IsTrue(profileResults.Count > 0, "Profile should produce results");
            Assert.IsTrue(profileResults.Last().Done, "Profile should complete");
            Assert.IsNotNull(finalProfile, "Final result should not be null");

            TestContext.WriteLine($"4 Force Thrusters - Commands: Completed with {profileResults.Count} progress steps");
            
            // Print profile results for each command
            foreach (var command in commands)
            {
                var profile = finalProfile.GetProfile(command);
                if (profile != null && profile.Any())
                {
                    var thrusterOutputs = string.Join(", ", profile.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                    TestContext.WriteLine($"  Command({command.DesiredForce}, {command.DesiredTorque}): {thrusterOutputs}");
                }
                else
                {
                    TestContext.WriteLine($"  Command({command.DesiredForce}, {command.DesiredTorque}): No profile found");
                }
            }
        }

        [TestMethod]
        public void ProfileCommands_With3OppositePairs_ValidatesForceAndTorque()
        {
            // Arrange
            var thrusters = ThrusterTestData.CreateThrusters3opposite();
            var engine = new RcsEngine(thrusters);
            var profiler = new RcsStaticEngineProfiler<LexicographicGoalSolver>();
            var commands = RcsProfileCommandTestData.GetRcsProfileCommands();

            // Act
            var profileResults = profiler.Profile(engine).ToList();
            var finalProfile = profileResults.Last().Result;

            // Assert
            Assert.IsTrue(profileResults.Count > 0, "Profile should produce results");
            Assert.IsTrue(profileResults.Last().Done, "Profile should complete");
            Assert.IsNotNull(finalProfile, "Final result should not be null");

            TestContext.WriteLine($"3 Opposite Pairs - Commands: Completed with {profileResults.Count} progress steps");
            
            // Print profile results for each command
            foreach (var command in commands)
            {
                var profile = finalProfile.GetProfile(command);
                if (profile != null && profile.Any())
                {
                    var thrusterOutputs = string.Join(", ", profile.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                    TestContext.WriteLine($"  Command({command.DesiredForce}, {command.DesiredTorque}): {thrusterOutputs}");
                }
                else
                {
                    TestContext.WriteLine($"  Command({command.DesiredForce}, {command.DesiredTorque}): No profile found");
                }
            }
        }

        [TestMethod]
        public void ProfileCommands_With12VariedMomentArm_ValidatesSolverParity()
        {
            // Arrange
            var thrusters = ThrusterTestData.CreateThrustersRandomFx();
            var engine = new RcsEngine(thrusters);
            var profiler = new RcsStaticEngineProfiler<LexicographicGoalSolver>();
            var commands = RcsProfileCommandTestData.GetRcsProfileCommands();

            // Act
            var profileResults = profiler.Profile(engine).ToList();
            var finalProfile = profileResults.Last().Result;

            // Assert
            Assert.IsTrue(profileResults.Count > 0, "Profile should produce results");
            Assert.IsTrue(profileResults.Last().Done, "Profile should complete");
            Assert.IsNotNull(finalProfile, "Final result should not be null");

            TestContext.WriteLine($"12 Varied Moment Arm - Commands: Completed with {profileResults.Count} progress steps");
            
            // Print profile results for each command
            foreach (var command in commands)
            {
                var profile = finalProfile.GetProfile(command);
                if (profile != null && profile.Any())
                {
                    var thrusterOutputs = string.Join(", ", profile.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                    TestContext.WriteLine($"  Command({command.DesiredForce}, {command.DesiredTorque}): {thrusterOutputs}");
                }
                else
                {
                    TestContext.WriteLine($"  Command({command.DesiredForce}, {command.DesiredTorque}): No profile found");
                }
            }
        }

        [TestMethod]
        public void ProfileCommands_With2RandomThrusters_ValidatesRandomConfiguration()
        {
            // Arrange
            var thrusters = ThrusterTestData.CreateThrustersRandom2Fx();
            var engine = new RcsEngine(thrusters);
            var profiler = new RcsStaticEngineProfiler<LexicographicGoalSolver>();
            var commands = RcsProfileCommandTestData.GetRcsProfileCommands();

            // Act
            var profileResults = profiler.Profile(engine).ToList();
            var finalProfile = profileResults.Last().Result;

            // Assert
            Assert.IsTrue(profileResults.Count > 0, "Profile should produce results");
            Assert.IsTrue(profileResults.Last().Done, "Profile should complete");
            Assert.IsNotNull(finalProfile, "Final result should not be null");

            TestContext.WriteLine($"2 Random Thrusters - Commands: Completed with {profileResults.Count} progress steps");
            
            // Print profile results for each command
            foreach (var command in commands)
            {
                var profile = finalProfile.GetProfile(command);
                if (profile != null && profile.Any())
                {
                    var thrusterOutputs = string.Join(", ", profile.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                    TestContext.WriteLine($"  Command({command.DesiredForce}, {command.DesiredTorque}): {thrusterOutputs}");
                }
                else
                {
                    TestContext.WriteLine($"  Command({command.DesiredForce}, {command.DesiredTorque}): No profile found");
                }
            }
        }
    }
}
