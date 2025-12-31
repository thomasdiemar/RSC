using System;
using System.Collections.Generic;
using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Diagnostics;
using LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Model;
using LinearSolver;

namespace LinearSolver.Custom.GoalProgramming.PreEmptive.BoundedInteger.Simplex
{
    /// <summary>
    /// Depth-first branch-and-bound search that tightens variable bounds until all integer variables hold integral values.
    /// </summary>
    public sealed class BoundedIntegerBranchAndBound
    {
        private const int MaxDepth = 32;
        public readonly BoundedIntegerSimplex simplex;

        public BoundedIntegerBranchAndBound(BoundedIntegerSimplex simplex)
        {
            this.simplex = simplex ?? throw new ArgumentNullException(nameof(simplex));
        }

        public SimplexResult EnforceIntegrality(PreEmptiveIntegerTableau tableau, int priority)
        {
            IReadOnlyList<BoundedIntegerVariable> incumbentSolution = null;
            var incumbentObjective = Fraction.MinValue;
            var aggregateDiagnostics = new SimplexDiagnostics(priority);
            var searchStack = new Stack<SearchNode>();
            searchStack.Push(new SearchNode(tableau.Clone(), depth: 0));

            while (searchStack.Count > 0)
            {
                var node = searchStack.Pop();
                var nodeTableau = node.Tableau;

                SimplexResult nodeResult;
                try
                {
                    nodeResult = simplex.SolvePriority(nodeTableau, priority);
                }
                catch (ArgumentException)
                {
                    continue;
                }

                aggregateDiagnostics.Merge(nodeResult.Diagnostics);

                if (nodeResult.Status != SimplexStatus.Optimal)
                {
                    continue;
                }

                var nodeUpperBound = nodeResult.ObjectiveValue + EstimateRemainingGain(nodeResult.Solution, nodeTableau, priority);
                aggregateDiagnostics.RecordUpperBound(nodeUpperBound);

                if (incumbentSolution != null && nodeUpperBound <= incumbentObjective)
                {
                    continue;
                }

                var fractionalIndex = SelectBranchVariable(nodeResult.Solution, nodeTableau, priority);
                if (fractionalIndex < 0)
                {
                    incumbentObjective = nodeResult.ObjectiveValue;
                    incumbentSolution = CloneSolution(nodeResult.Solution);
                    tableau.ApplySolution(incumbentSolution);
                    aggregateDiagnostics.RecordIncumbent(incumbentObjective);
                    aggregateDiagnostics.SetFinalStates(incumbentSolution);
                    continue;
                }

                if (node.Depth >= MaxDepth)
                {
                    continue;
                }

                var variable = nodeResult.Solution[fractionalIndex];
                var floor = BoundedIntegerVariable.Floor(variable.Value);
                var ceil = BoundedIntegerVariable.Ceiling(variable.Value);

                if (floor >= variable.LowerBound)
                {
                    var left = nodeTableau.Clone();
                    left.ApplyBoundOverride(fractionalIndex, variable.LowerBound, floor);
                    searchStack.Push(new SearchNode(left, node.Depth + 1));
                    aggregateDiagnostics.RecordBranch(fractionalIndex, node.Depth + 1);
                    aggregateDiagnostics.RecordBranchDetail(variable.Name, variable.LowerBound, floor, node.Depth + 1, floor - variable.LowerBound);
                }

                if (ceil <= variable.UpperBound)
                {
                    var right = nodeTableau.Clone();
                    right.ApplyBoundOverride(fractionalIndex, ceil, variable.UpperBound);
                    searchStack.Push(new SearchNode(right, node.Depth + 1));
                    aggregateDiagnostics.RecordBranch(fractionalIndex, node.Depth + 1);
                    aggregateDiagnostics.RecordBranchDetail(variable.Name, ceil, variable.UpperBound, node.Depth + 1, variable.UpperBound - ceil);
                }
            }

            if (incumbentSolution != null)
            {
                aggregateDiagnostics.SetFinalStates(incumbentSolution);
                return new SimplexResult(SimplexStatus.Optimal, incumbentObjective, aggregateDiagnostics, incumbentSolution);
            }

            return new SimplexResult(SimplexStatus.GoalViolation, new Fraction(0), aggregateDiagnostics, tableau.ColumnHeaders);
        }

        private static int SelectBranchVariable(IReadOnlyList<BoundedIntegerVariable> variables, PreEmptiveIntegerTableau tableau, int priority)
        {
            var bestIndex = -1;
            var bestImpact = new Fraction(-1);
            var bestDistance = new Fraction(-1);

            for (int i = 0; i < variables.Count; i++)
            {
                var variable = variables[i];
                if (!variable.IsInteger || variable.HasIntegralValue())
                {
                    continue;
                }

                var distance = variable.FractionalDistance();
                if (distance == new Fraction(0))
                {
                    continue;
                }

                var weight = GetPriorityWeight(tableau, priority, i);
                var impact = weight * distance;
                if (impact > bestImpact || (impact == bestImpact && distance > bestDistance))
                {
                    bestImpact = impact;
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static Fraction EstimateRemainingGain(IReadOnlyList<BoundedIntegerVariable> variables, PreEmptiveIntegerTableau tableau, int priority)
        {
            var gain = new Fraction(0);
            for (int i = 0; i < variables.Count; i++)
            {
                var variable = variables[i];
                if (!variable.IsInteger || variable.HasIntegralValue())
                {
                    continue;
                }

                var distance = variable.FractionalDistance();
                var weight = GetPriorityWeight(tableau, priority, i);
                gain += distance * weight;
            }

            return gain;
        }

        private static Fraction GetPriorityWeight(PreEmptiveIntegerTableau tableau, int priority, int columnIndex)
        {
            var weight = new Fraction(0);
            for (int row = 0; row < tableau.RowCount; row++)
            {
                if (tableau.RowGoals[row].Priority != priority)
                {
                    continue;
                }

                var coefficient = tableau.GetCoefficient(row, columnIndex);
                weight += Fraction.Abs(coefficient);
            }

            return weight;
        }

        private sealed class SearchNode
        {
            public SearchNode(PreEmptiveIntegerTableau tableau, int depth)
            {
                Tableau = tableau;
                Depth = depth;
            }

            public PreEmptiveIntegerTableau Tableau { get; }

            public int Depth { get; }
        }

        private static IReadOnlyList<BoundedIntegerVariable> CloneSolution(IReadOnlyList<BoundedIntegerVariable> solution)
        {
            var list = new List<BoundedIntegerVariable>(solution.Count);
            for (int i = 0; i < solution.Count; i++)
            {
                list.Add(solution[i].Clone());
            }

            return list;
        }
    }
}
