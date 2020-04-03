using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Threading;
using System.Threading.Tasks;
using System.Linq;

/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { __out.Add(text); }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { __out.Add(string.Format(format, args)); }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj)); }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj, method_name)); }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private IGH_Component Component; 
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments, 
  /// Output parameters as ref arguments. You don't have to assign output parameters, 
  /// they will have a default value.
  /// </summary>
  private void RunScript(Point3d P0, int n, double freq, double amp, double speed, bool reset, bool go, int MT, bool GHtype, ref object P)
  {
        if (reset || ptsArray == null || ptsArray.Length != n * n)
    {
      ptsArray = initPts(n);
      c = 0;
    }

    switch (MT)
    {
      case 0:
        for(int i = 0; i < ptsArray.Length; i++)
        {
          double dd = ptsArray[i].DistanceToSquared(P0);
          ptsArray[i].Z = Math.Cos(dd * ptsArray[i].X * freq * 0.001 + c * speed) * Math.Sin(dd * ptsArray[i].Y * freq * 0.001 + c * speed * 0.43) * amp;
        }
        break;

      case 1:
        Parallel.For(0, ptsArray.Length, i =>
          {
          double dd = ptsArray[i].DistanceToSquared(P0);
          ptsArray[i].Z = Math.Cos(dd * ptsArray[i].X * freq * 0.001 + c * speed) * Math.Sin(dd * ptsArray[i].Y * freq * 0.001 + c * speed * 0.43) * amp;
          });
        break;

      //      case 2:
      // Parallel.Foreach does not work with arrays (list only?)
      //        Parallel.ForEach(ptsArray, pt =>
      //          {
      //          double dd = pt.DistanceToSquared(P0);
      //          pt.Z = Math.Cos(dd * pt.X * freq * 0.001 + c * speed) * Math.Sin(dd * pt.Y * freq * 0.001 + c * speed * 0.43) * amp;
      //          });
      //        break;

      case 2:
        var  chunks = System.Collections.Concurrent.Partitioner.Create(0, ptsArray.Length);
        Parallel.ForEach(chunks, range =>
          {
          for(int i = range.Item1; i < range.Item2; i++)
          {
            double dd = ptsArray[i].DistanceToSquared(P0);
            ptsArray[i].Z = Math.Cos(dd * ptsArray[i].X * freq * 0.001 + c * speed) * Math.Sin(dd * ptsArray[i].Y * freq * 0.001 + c * speed * 0.43) * amp;
          }
          });
        break;
    }

    if (go)
    {
      c++;
      Component.ExpireSolution(true);
    }

    if(GHtype)
      P = ptsArray.Select(x => new GH_Point(x));
    else P = ptsArray;
  }

  // <Custom additional code> 
    Point3d[] ptsArray;
  int c;

  Point3d[] initPts(int n)
  {
    Point3d[] ptsArray = new Point3d[n * n];

    for(int i = 0; i < n; i++)
      for(int j = 0; j < n; j++)
        ptsArray[i * n + j] = new Point3d(i, j, 0);
    return ptsArray;
  }

  // </Custom additional code> 

  private List<string> __err = new List<string>(); //Do not modify this list directly.
  private List<string> __out = new List<string>(); //Do not modify this list directly.
  private RhinoDoc doc = RhinoDoc.ActiveDoc;       //Legacy field.
  private IGH_ActiveObject owner;                  //Legacy field.
  private int runCount;                            //Legacy field.
  
  public override void InvokeRunScript(IGH_Component owner, object rhinoDocument, int iteration, List<object> inputs, IGH_DataAccess DA)
  {
    //Prepare for a new run...
    //1. Reset lists
    this.__out.Clear();
    this.__err.Clear();

    this.Component = owner;
    this.Iteration = iteration;
    this.GrasshopperDocument = owner.OnPingDocument();
    this.RhinoDocument = rhinoDocument as Rhino.RhinoDoc;

    this.owner = this.Component;
    this.runCount = this.Iteration;
    this. doc = this.RhinoDocument;

    //2. Assign input parameters
        Point3d P0 = default(Point3d);
    if (inputs[0] != null)
    {
      P0 = (Point3d)(inputs[0]);
    }

    int n = default(int);
    if (inputs[1] != null)
    {
      n = (int)(inputs[1]);
    }

    double freq = default(double);
    if (inputs[2] != null)
    {
      freq = (double)(inputs[2]);
    }

    double amp = default(double);
    if (inputs[3] != null)
    {
      amp = (double)(inputs[3]);
    }

    double speed = default(double);
    if (inputs[4] != null)
    {
      speed = (double)(inputs[4]);
    }

    bool reset = default(bool);
    if (inputs[5] != null)
    {
      reset = (bool)(inputs[5]);
    }

    bool go = default(bool);
    if (inputs[6] != null)
    {
      go = (bool)(inputs[6]);
    }

    int MT = default(int);
    if (inputs[7] != null)
    {
      MT = (int)(inputs[7]);
    }

    bool GHtype = default(bool);
    if (inputs[8] != null)
    {
      GHtype = (bool)(inputs[8]);
    }



    //3. Declare output parameters
      object P = null;


    //4. Invoke RunScript
    RunScript(P0, n, freq, amp, speed, reset, go, MT, GHtype, ref P);
      
    try
    {
      //5. Assign output parameters to component...
            if (P != null)
      {
        if (GH_Format.TreatAsCollection(P))
        {
          IEnumerable __enum_P = (IEnumerable)(P);
          DA.SetDataList(1, __enum_P);
        }
        else
        {
          if (P is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(P));
          }
          else
          {
            //assign direct
            DA.SetData(1, P);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
      }

    }
    catch (Exception ex)
    {
      this.__err.Add(string.Format("Script exception: {0}", ex.Message));
    }
    finally
    {
      //Add errors and messages... 
      if (owner.Params.Output.Count > 0)
      {
        if (owner.Params.Output[0] is Grasshopper.Kernel.Parameters.Param_String)
        {
          List<string> __errors_plus_messages = new List<string>();
          if (this.__err != null) { __errors_plus_messages.AddRange(this.__err); }
          if (this.__out != null) { __errors_plus_messages.AddRange(this.__out); }
          if (__errors_plus_messages.Count > 0) 
            DA.SetDataList(0, __errors_plus_messages);
        }
      }
    }
  }
}