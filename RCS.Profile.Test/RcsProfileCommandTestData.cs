using LinearSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCS.Profile.Test
{
    public static class RcsProfileCommandTestData
    {
        public static List<RcsProfileCommand> GetRcsProfileCommands()
        {
            return new List<RcsProfileCommand> {
                // ========== INTEGER COMMANDS (±1) ==========
                
                // Single-axis force commands (6)
                new RcsProfileCommand(new RcsVector<float>(1f, 0f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-1f, 0f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 1f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, -1f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, -1f), new RcsVector<float>(0f, 0f, 0f)),
                
                // Dual-axis force commands - XY plane (4)
                new RcsProfileCommand(new RcsVector<float>(1f, 1f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(1f, -1f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-1f, 1f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-1f, -1f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                
                // Dual-axis force commands - XZ plane (4)
                new RcsProfileCommand(new RcsVector<float>(1f, 0f, 1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(1f, 0f, -1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-1f, 0f, 1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-1f, 0f, -1f), new RcsVector<float>(0f, 0f, 0f)),
                
                // Dual-axis force commands - YZ plane (4)
                new RcsProfileCommand(new RcsVector<float>(0f, 1f, 1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 1f, -1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, -1f, 1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, -1f, -1f), new RcsVector<float>(0f, 0f, 0f)),
                
                // Triple-axis force commands (8)
                new RcsProfileCommand(new RcsVector<float>(1f, 1f, 1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(1f, 1f, -1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(1f, -1f, 1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(1f, -1f, -1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-1f, 1f, 1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-1f, 1f, -1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-1f, -1f, 1f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-1f, -1f, -1f), new RcsVector<float>(0f, 0f, 0f)),
                
                // Single-axis torque commands (6)
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(1f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-1f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, 1f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, -1f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, 0f, 1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, 0f, -1f)),
                
                // Dual-axis torque commands - XY plane (4)
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(1f, 1f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(1f, -1f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-1f, 1f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-1f, -1f, 0f)),
                
                // Dual-axis torque commands - XZ plane (4)
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(1f, 0f, 1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(1f, 0f, -1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-1f, 0f, 1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-1f, 0f, -1f)),
                
                // Dual-axis torque commands - YZ plane (4)
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, 1f, 1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, 1f, -1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, -1f, 1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, -1f, -1f)),
                
                // Triple-axis torque commands (8)
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(1f, 1f, 1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(1f, 1f, -1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(1f, -1f, 1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(1f, -1f, -1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-1f, 1f, 1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-1f, 1f, -1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-1f, -1f, 1f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-1f, -1f, -1f)),
                
                // ========== FRACTIONAL COMMANDS (±0.5) ==========
                
                // Single-axis force commands with 0.5 (6)
                new RcsProfileCommand(new RcsVector<float>(0.5f, 0f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-0.5f, 0f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0.5f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, -0.5f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, -0.5f), new RcsVector<float>(0f, 0f, 0f)),
                
                // Dual-axis force commands with 0.5 - XY plane (4)
                new RcsProfileCommand(new RcsVector<float>(0.5f, 0.5f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0.5f, -0.5f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-0.5f, 0.5f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-0.5f, -0.5f, 0f), new RcsVector<float>(0f, 0f, 0f)),
                
                // Dual-axis force commands with 0.5 - XZ plane (4)
                new RcsProfileCommand(new RcsVector<float>(0.5f, 0f, 0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0.5f, 0f, -0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-0.5f, 0f, 0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-0.5f, 0f, -0.5f), new RcsVector<float>(0f, 0f, 0f)),
                
                // Dual-axis force commands with 0.5 - YZ plane (4)
                new RcsProfileCommand(new RcsVector<float>(0f, 0.5f, 0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0.5f, -0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, -0.5f, 0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, -0.5f, -0.5f), new RcsVector<float>(0f, 0f, 0f)),
                
                // Triple-axis force commands with 0.5 (8)
                new RcsProfileCommand(new RcsVector<float>(0.5f, 0.5f, 0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0.5f, 0.5f, -0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0.5f, -0.5f, 0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0.5f, -0.5f, -0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-0.5f, 0.5f, 0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-0.5f, 0.5f, -0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-0.5f, -0.5f, 0.5f), new RcsVector<float>(0f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(-0.5f, -0.5f, -0.5f), new RcsVector<float>(0f, 0f, 0f)),
                
                // Single-axis torque commands with 0.5 (6)
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0.5f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-0.5f, 0f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, 0.5f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, -0.5f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, 0f, 0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, 0f, -0.5f)),
                
                // Dual-axis torque commands with 0.5 - XY plane (4)
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0.5f, 0.5f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0.5f, -0.5f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-0.5f, 0.5f, 0f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-0.5f, -0.5f, 0f)),
                
                // Dual-axis torque commands with 0.5 - XZ plane (4)
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0.5f, 0f, 0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0.5f, 0f, -0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-0.5f, 0f, 0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-0.5f, 0f, -0.5f)),
                
                // Dual-axis torque commands with 0.5 - YZ plane (4)
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, 0.5f, 0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, 0.5f, -0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, -0.5f, 0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0f, -0.5f, -0.5f)),
                
                // Triple-axis torque commands with 0.5 (8)
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0.5f, 0.5f, 0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0.5f, 0.5f, -0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0.5f, -0.5f, 0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(0.5f, -0.5f, -0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-0.5f, 0.5f, 0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-0.5f, 0.5f, -0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-0.5f, -0.5f, 0.5f)),
                new RcsProfileCommand(new RcsVector<float>(0f, 0f, 0f), new RcsVector<float>(-0.5f, -0.5f, -0.5f))
            };
        }
    }
}

