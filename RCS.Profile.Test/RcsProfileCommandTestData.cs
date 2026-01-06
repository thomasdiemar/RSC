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
                new RcsProfileCommand(new RcsVector<Fraction>(1, 0, 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(-1, 0, 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, 1, 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, -1, 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, 0, 1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, 0, -1), RcsProfileCommand.NOCOMMAND),
                
                // Dual-axis force commands - XY plane (4)
                new RcsProfileCommand(new RcsVector<Fraction>(1, 1, 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(1, -1, 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(-1, 1, 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(-1, -1, 0), RcsProfileCommand.NOCOMMAND),
                
                // Dual-axis force commands - XZ plane (4)
                new RcsProfileCommand(new RcsVector<Fraction>(1, 0, 1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(1, 0, -1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(-1, 0, 1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(-1, 0, -1), RcsProfileCommand.NOCOMMAND),
                
                // Dual-axis force commands - YZ plane (4)
                new RcsProfileCommand(new RcsVector<Fraction>(0, 1, 1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, 1, -1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, -1, 1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, -1, -1), RcsProfileCommand.NOCOMMAND),
                
                // Triple-axis force commands (8)
                new RcsProfileCommand(new RcsVector<Fraction>(1, 1, 1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(1, 1, -1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(1, -1, 1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(1, -1, -1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(-1, 1, 1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(-1, 1, -1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(-1, -1, 1), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(-1, -1, -1), RcsProfileCommand.NOCOMMAND),
                
                // Single-axis torque commands (6)
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(1, 0, 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(-1, 0, 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, 1, 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, -1, 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, 0, 1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, 0, -1)),
                
                // Dual-axis torque commands - XY plane (4)
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(1, 1, 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(1, -1, 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(-1, 1, 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(-1, -1, 0)),
                
                // Dual-axis torque commands - XZ plane (4)
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(1, 0, 1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(1, 0, -1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(-1, 0, 1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(-1, 0, -1)),
                
                // Dual-axis torque commands - YZ plane (4)
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, 1, 1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, 1, -1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, -1, 1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, -1, -1)),
                
                // Triple-axis torque commands (8)
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(1, 1, 1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(1, 1, -1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(1, -1, 1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(1, -1, -1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(-1, 1, 1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(-1, 1, -1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(-1, -1, 1)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(-1, -1, -1)),
                
                // ========== FRACTIONAL COMMANDS (±0.5) ==========
                
                // Single-axis force commands with 0.5 (6)
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(1, 2), 0, 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(-1, 2), 0, 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, new Fraction(1, 2), 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, new Fraction(-1, 2), 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, 0, new Fraction(1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, 0, new Fraction(-1, 2)), RcsProfileCommand.NOCOMMAND),
                
                // Dual-axis force commands with 0.5 - XY plane (4)
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(1, 2), new Fraction(1, 2), 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(1, 2), new Fraction(-1, 2), 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(-1, 2), new Fraction(1, 2), 0), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(-1, 2), new Fraction(-1, 2), 0), RcsProfileCommand.NOCOMMAND),
                
                // Dual-axis force commands with 0.5 - XZ plane (4)
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(1, 2), 0, new Fraction(1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(1, 2), 0, new Fraction(-1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(-1, 2), 0, new Fraction(1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(-1, 2), 0, new Fraction(-1, 2)), RcsProfileCommand.NOCOMMAND),
                
                // Dual-axis force commands with 0.5 - YZ plane (4)
                new RcsProfileCommand(new RcsVector<Fraction>(0, new Fraction(1, 2), new Fraction(1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, new Fraction(1, 2), new Fraction(-1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, new Fraction(-1, 2), new Fraction(1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(0, new Fraction(-1, 2), new Fraction(-1, 2)), RcsProfileCommand.NOCOMMAND),
                
                // Triple-axis force commands with 0.5 (8)
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(1, 2), new Fraction(1, 2), new Fraction(1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(1, 2), new Fraction(1, 2), new Fraction(-1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(1, 2), new Fraction(-1, 2), new Fraction(1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(1, 2), new Fraction(-1, 2), new Fraction(-1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(-1, 2), new Fraction(1, 2), new Fraction(1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(-1, 2), new Fraction(1, 2), new Fraction(-1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(-1, 2), new Fraction(-1, 2), new Fraction(1, 2)), RcsProfileCommand.NOCOMMAND),
                new RcsProfileCommand(new RcsVector<Fraction>(new Fraction(-1, 2), new Fraction(-1, 2), new Fraction(-1, 2)), RcsProfileCommand.NOCOMMAND),
                
                // Single-axis torque commands with 0.5 (6)
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(1, 2), 0, 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(-1, 2), 0, 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, new Fraction(1, 2), 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, new Fraction(-1, 2), 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, 0, new Fraction(1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, 0, new Fraction(-1, 2))),
                
                // Dual-axis torque commands with 0.5 - XY plane (4)
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(1, 2), new Fraction(1, 2), 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(1, 2), new Fraction(-1, 2), 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(-1, 2), new Fraction(1, 2), 0)),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(-1, 2), new Fraction(-1, 2), 0)),
                
                // Dual-axis torque commands with 0.5 - XZ plane (4)
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(1, 2), 0, new Fraction(1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(1, 2), 0, new Fraction(-1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(-1, 2), 0, new Fraction(1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(-1, 2), 0, new Fraction(-1, 2))),
                
                // Dual-axis torque commands with 0.5 - YZ plane (4)
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, new Fraction(1, 2), new Fraction(1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, new Fraction(1, 2), new Fraction(-1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, new Fraction(-1, 2), new Fraction(1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(0, new Fraction(-1, 2), new Fraction(-1, 2))),
                
                // Triple-axis torque commands with 0.5 (8)
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(1, 2), new Fraction(1, 2), new Fraction(1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(1, 2), new Fraction(1, 2), new Fraction(-1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(1, 2), new Fraction(-1, 2), new Fraction(1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(1, 2), new Fraction(-1, 2), new Fraction(-1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(-1, 2), new Fraction(1, 2), new Fraction(1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(-1, 2), new Fraction(1, 2), new Fraction(-1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(-1, 2), new Fraction(-1, 2), new Fraction(1, 2))),
                new RcsProfileCommand(RcsProfileCommand.NOCOMMAND, new RcsVector<Fraction>(new Fraction(-1, 2), new Fraction(-1, 2), new Fraction(-1, 2)))
            };
        }
    }
}
