/*  Copyright 2021-2025 MarcosHCK
 *  This file is part of Algebra!.
 *
 *  Algebra! is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Algebra! is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Algebra!. If not, see <http://www.gnu.org/licenses/>.
 *
 */
using System.Text;

namespace Algebra
{
  [System.Serializable]
  public class MatrixException : System.Exception
  {
    public MatrixException() { }
    public MatrixException(string message) : base(message) { }
    public MatrixException(string message, System.Exception inner) : base(message, inner) { }
    protected MatrixException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
  }

  public class Matrix<T> where T : notnull
  {
#region Internal data structures

    private T[,] data;
    private bool transposed = true;
    public int n {
      get {
        return data.GetLength(transposed ? 1 : 0);
      }}
    public int m {
      get {
        return data.GetLength(transposed ? 0 : 1);
      }}
    public T this[int n, int m] {
      get {
        return this.data[(transposed) ? m : n, (transposed) ? n : m];
      }
      set {
        this.data[(transposed) ? m : n, (transposed) ? n : m] = value;
      }}

#endregion

#region Helpers

    private static T zero()
    {
      return default(T)!;
    }

    private static T plus(T a, T b)
    {
      dynamic a_ = (dynamic) a;
      dynamic b_ = (dynamic) b;
      return (T) (a_ + b_);
    }

    private static T subs(T a, T b)
    {
      dynamic a_ = (dynamic) a;
      dynamic b_ = (dynamic) b;
      return (T) (a_ - b_);
    }

    private static T dot(T a, T b)
    {
      dynamic a_ = (dynamic) a;
      dynamic b_ = (dynamic) b;
      return (T) (a_ * b_);
    }

    private static T scale(T a, decimal b)
    {
      dynamic a_ = (dynamic) a;
    return (T) (a_ * b);
    }

    private static decimal asdecimal(T a)
    {
      dynamic a_ = (dynamic) a;
    return (decimal) a_;
    }

#endregion

#region Overloads

    public override string ToString()
    {
      var str = new StringBuilder();
      int i, j;

      for (i = 0; i < this.n; i++)
      {
        str.Append("|");

        for (j = 0; j < this.m; j++)
        {
          str.AppendFormat(" {0} ", (this[i, j]).ToString());
        }

        str.Append("|\r\n");
      }
      return str.ToString();
    }

#endregion

#region Operators

    public static Matrix<T> operator+(Matrix<T> a, Matrix<T> b)
    {
      if (a.n != b.n || a.m != b.m)
      {
        throw new MatrixException("Matrix must be equal");
      }
      else
      {
        var data = new T[a.n, a.m];
        int i, j;

        for (i = 0; i < a.n; i++)
          for (j = 0; j < a.m; j++)
          {
            data[i, j] = plus(a[i, j], b[i, j]);
          }

        return new Matrix<T>(data);
      }
    }

    public static Matrix<T> operator*(Matrix<T> a, Matrix<T> b)
    {
      if (a.n != b.m || a.m != b.n)
      {
        throw new MatrixException("Matrix must be equal");
      }
      else
      {
        var data = new T[a.n, b.m];
        int i, j, k;

        for (i = 0; i < a.n; i++)
        {
          for (j = 0, j = 0; j < b.m; j++)
          {
            var element = zero();
            for (k = 0; k < a.m; k++)
            {
              var prod = dot(a[i, k], b[k, j]);
              element = plus(element, prod);
            }

            data[i, j] = element;
          }
        }

        return new Matrix<T>(data);
      }
    }

    public static Matrix<T> operator*(Matrix<T> a, decimal scalar)
    {
      var b = new Matrix<T>(a.data);
      int i, j;

      for(i = 0; i < a.n; i++)
      {
        for(j = 0; j < a.m; j++)
        {
          b[i, j] = scale(a[i, j], scalar);
        }
      }
    return b;
    }

#endregion

#region Methods

    public decimal Det()
    {
      if(n != m)
        throw new MatrixException("Determinants are only valid on square matrices");
      decimal det = 0;
      int i, j, k;

      switch(n)
      {
      case 1:
        return asdecimal(this[0, 0]);
      case 2:
        var semi1_2 = dot(this[0, 0], this[1, 1]);
        var semi2_2 = dot(this[1, 0], this[0, 1]);
        var demi_2 = subs(semi1_2, semi2_2);
        return asdecimal(demi_2);
      case 3:
        var semi1_3 = dot(dot(this[0, 0], this[1, 1]), this[2, 2]);
        var semi2_3 = dot(dot(this[0, 1], this[1, 2]), this[2, 0]);
        var semi3_3 = dot(dot(this[0, 2], this[1, 0]), this[2, 1]);
        var semi4_3 = dot(dot(this[2, 0], this[1, 1]), this[0, 2]);
        var semi5_3 = dot(dot(this[2, 1], this[1, 2]), this[0, 0]);
        var semi6_3 = dot(dot(this[2, 2], this[1, 0]), this[0, 1]);
        var demi7_3 = plus(plus(semi1_3, semi2_3), semi3_3);
        var demi_3 = subs(subs(subs(demi7_3, semi4_3), semi5_3), semi6_3);
        return asdecimal(demi_3);
      default:
        /*
         * This algorithm is hard-wired to be
         * a minor devolopment for determinant
         * calculation by column j=0
         *
         */
        for(k = 0; k < n; k++)
        {
          var minor = new T[n - 1, n - 1];
          int jump = 0;

          for(i = 0; i < (n - 1); i++)
          {
            if(i == k)
              jump++;

            for(j = 0; j < (n - 1); j++)
            {
              minor[i, j] = this[i + jump, j + 1];
            }
          }

          if((k & 1) == 0)
            det += asdecimal(this[k, 0]) * (new Matrix<T>(minor)).Det();
          else
            det -= asdecimal(this[k, 0]) * (new Matrix<T>(minor)).Det();
        }
        break;
      }
    return det;
    }

    public int Rg()
    {
    return 0;
    }

#endregion

#region Constructors

    private Matrix(T[,] data, bool ff)
    {
      this.data = data;
    }

    public Matrix(int n, int m) : this(new T[n, m], false) { }
    public Matrix(T[,] data) : this(data, false) { }
#endregion
  }
}
