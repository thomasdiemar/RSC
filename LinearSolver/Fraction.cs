// Author : Nikola Stepan
// E-mail : nikola.stepan@vz.htnet.hr
// Web    : http://calcsharp.net

// fractions are auto-reducing and immutable
// numerator and denominator must be in [-int.MaxValue, int.MaxValue]
// binary operator overloading methods can throw overflow exception

using System;


namespace LinearSolver
{
	public struct Fraction
	{
		long num;
		long den;

		#region Constructors

		//public Fraction()
		//{
		//    num = 0;
		//    den = 1;
		//    Initialize();
		//}

		public Fraction(int n) : this(n, 1)
		{
		}

		public Fraction(Fraction f) : this(f.num, f.den)
		{
		}

		public Fraction(int n, int d)
		{
			num = n;
			den = d == 0 && n == 0 ? 1 : d;

			CheckDenominatorZero(den);

			CheckMinValue(num);
			CheckMinValue(den);

			//Fraction f = new Fraction( (decimal)num, (decimal)den );
			//Initialize( );
		}

		private static void CheckMinValue(long n)
		{
			if (n == int.MinValue)
				throw new Exception();
		}

        //private void Initialize( int num, int den )
        //	{
        //	this.num = num;
        //	this.den = den;
        //	}

        public Fraction(decimal r1, decimal r2)
		{
			if (r2 == 0)
			{
				r2 = 1;
			}
			decimal gcd = GCD(Math.Abs(r1), Math.Abs(r2));

			long num = (long)(r1 / gcd);
			long den = (long)(r2 / gcd);

			CheckMinValue(num);
			CheckMinValue(den);

			if (r2 < 0)
			{
				num = -num;
				den = -den;
			}

			this.num = num;
			this.den = den;
		}

		#endregion

		#region Properties

		public int Numerator
		{
			get { return (int)this.num; }
		}

		public int Denominator
		{
			get { return (int)this.den; }
		}

		public static Fraction MinValue => new Fraction(int.MinValue + 1, 1);
		public static Fraction MaxValue => new Fraction(int.MaxValue - 1, 1);

        public static Fraction Zero => new Fraction(0, 1);

        #endregion

        #region Overloaded Binary Operators ( +-*/^ )

        #region Add

        public static Fraction operator +(Fraction a, Fraction b)
		{
			decimal r1 = (decimal)a.num * b.den + (decimal)b.num * a.den;
			decimal r2 = (decimal)a.den * b.den;
			return new Fraction(r1, r2);
		}

		public static Fraction operator +(Fraction a, int b)
		{
			return a + new Fraction(b);
		}

		public static Fraction operator +(int a, Fraction b)
		{
			return new Fraction(a) + b;
		}


		#endregion

		#region Substract

		public static Fraction operator -(Fraction a, Fraction b)
		{
			decimal r1 = (decimal)a.num * b.den - (decimal)b.num * a.den;
			decimal r2 = (decimal)a.den * b.den;
			return new Fraction(r1, r2);
		}

		public static Fraction operator -(Fraction a, int b)
		{
			return a - new Fraction(b);
		}

		public static Fraction operator -(int a, Fraction b)
		{
			return new Fraction(a) - b;
		}

		#endregion

		#region Multiply

		public static Fraction operator *(Fraction a, Fraction b)
		{
			decimal r1 = (decimal)a.num * b.num;
			decimal r2 = (decimal)a.den * b.den;
			return new Fraction(r1, r2);
		}

		public static Fraction operator *(Fraction a, int b)
		{
			return a * new Fraction(b);
		}

		public static Fraction operator *(int a, Fraction b)
		{
			return new Fraction(a) * b;
		}

		#endregion

		#region Divide

		public static Fraction operator /(Fraction a, Fraction b)
		{
			decimal r1 = (decimal)a.num * b.den;
			decimal r2 = (decimal)a.den * b.num;

			if (r2 == 0)
				throw new DivideByZeroException();
			else
				return new Fraction(r1, r2);
		}

		public static Fraction operator /(Fraction a, int b)
		{
			return a / new Fraction(b);
		}

		public static Fraction operator /(int a, Fraction b)
		{
			return new Fraction(a) / b;
		}

		#endregion

		#region Power

		public static Fraction operator ^(Fraction a, int n)
		{
			return new Fraction((decimal)Math.Pow(a.num, n), (decimal)Math.Pow(a.den, n));
		}

		#endregion

		#endregion

		#region Comparison operators

		public static bool operator ==(Fraction a, Fraction b)
		{
			return (decimal)a.num * b.den == (decimal)b.num * a.den;
		}

		public static bool operator !=(Fraction a, Fraction b)
		{
			return (!(a == b));
		}

		public static bool operator >(Fraction a, Fraction b)
		{
			return (decimal)a.num * b.den > (decimal)b.num * a.den;
		}

		public static bool operator >=(Fraction a, Fraction b)
		{
			return (!(a < b));
		}

		public static bool operator <(Fraction a, Fraction b)
		{
			return (decimal)a.num * b.den < (decimal)b.num * a.den;
		}

		public static bool operator <=(Fraction a, Fraction b)
		{
			return (!(a > b));
		}

		#endregion

		#region Misc

		public override string ToString()
		{
			if (this.den == 1)
				return this.num.ToString();
			else
				return this.num + "/" + this.den;
		}

		public override bool Equals(object o)
		{
			if (o == null || o.GetType() != GetType())
				return false;
			Fraction f = (Fraction)o;
			return (this == f);
		}

		public override int GetHashCode()
		{
			return (int)(this.num ^ this.den);
		}
		public static implicit operator double(Fraction d) => (double)d.num / (double)d.den;

		public static implicit operator Fraction(int b)
		{
			var f = new Fraction(b);
			return f;
		}

		// Euclid identities

		// GCD(A,B)=GCD(B,A) 
		// GCD(A,B)=GCD(-A,B) 
		// GCD(A,0)=ABS(A) 
		// GCD(0,0)=0
		// GCD(A,B)=GCD(B,A%B)

		public static decimal GCD(decimal a, decimal b)
		{
			if (b == 0)
				return a;
			return GCD(b, a % b);
		}

		private static void CheckDenominatorZero(decimal den)
		{
			if (den == 0)
				throw new Exception("The denominator of any fraction cannot have the value zero");
		}

		// throws FormatException if wrong fraction format
		// throws OverflowException if reduced fraction does not fit in fraction range
		// throws ArithmeticException if denominator is zero
		public static Fraction Parse(string fraction)
		{
			if (fraction == null)
				throw new FormatException();

			string[] split = fraction.Split('/');
			int len = split.Length;

			if (len == 2)
			{
				int s0 = int.Parse(split[0]);
				int s1 = int.Parse(split[1]);
				return new Fraction(s0, s1);
			}
			else if (len == 4)
			{
				int s0 = int.Parse(split[0]);
				int s1 = int.Parse(split[1]);
				Fraction f1 = new Fraction(s0, s1);

				int s2 = int.Parse(split[2]);
				int s3 = int.Parse(split[3]);
				Fraction f2 = new Fraction(s2, s3);

				return f1 / f2;
			}
			else
				throw new FormatException();
		}

		public Fraction Inverse()
		{
			return new Fraction(this.den, this.num);
		}

		public static Fraction Min(Fraction a, Fraction b)
		{
			return a < b ? a : b;
		}

		public static bool IsNaN(Fraction a)
		{
			return a.Denominator == 0;
		}

		public static bool IsInfinity(Fraction a)
		{
			return a.Denominator == int.MaxValue
				|| a.Denominator == int.MinValue
				|| a.Numerator == int.MaxValue
				|| a.Numerator == int.MinValue;
		}

		public static Fraction Abs(Fraction a)
		{
			return new Fraction(Math.Abs(a.Numerator), Math.Abs(a.Denominator));
		}

        public double ToDouble()
        {
			return num / den;
        }

        public static Fraction Epsilon = new Fraction(1, 100000000);

		public static readonly Fraction NaN = new Fraction{ num = 0, den = 0};

        #endregion



    }
}
