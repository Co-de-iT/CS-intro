using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.IO;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;

using Rhino.DocObjects;
using Rhino.Collections;
using GH_IO;
using GH_IO.Serialization;

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
  private void RunScript(Mesh M, Mesh V, int n, int s, ref object Pts)
  {
        Random r = new Random(s);
    BoundingBox b = new BoundingBox(M.Vertices.ToPoint3dArray());
    Point3d max = b.Max;
    Point3d min = b.Min;
    double x,y,z;
    int count = 0, i = 0;
    List<Point3d> pts = new List<Point3d>();
    List<Point3d> ptsOut = new List<Point3d>();

    while (count < n){

      x = b.Min.X + r.NextDouble() * (b.Max.X - b.Min.X);
      y = b.Min.Y + r.NextDouble() * (b.Max.Y - b.Min.Y);
      z = b.Min.Z + r.NextDouble() * (b.Max.Z - b.Min.Z);
      Point3d p = new Point3d(x, y, z);

      if (M.IsPointInside(p, 0.1, false) && (V == null ? true : !V.IsPointInside(p, 0.1, false))){
        pts.Add(p);
        count++;
      } else{
        ptsOut.Add(p);
      }

      i++;
    }

    Pts = pts;
  }

  // <Custom additional code> 
  
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
        Mesh M = default(Mesh);
    if (inputs[0] != null)
    {
      M = (Mesh)(inputs[0]);
    }

    Mesh V = default(Mesh);
    if (inputs[1] != null)
    {
      V = (Mesh)(inputs[1]);
    }

    int n = default(int);
    if (inputs[2] != null)
    {
      n = (int)(inputs[2]);
    }

    int s = default(int);
    if (inputs[3] != null)
    {
      s = (int)(inputs[3]);
    }



    //3. Declare output parameters
      object Pts = null;


    //4. Invoke RunScript
    RunScript(M, V, n, s, ref Pts);
      
    try
    {
      //5. Assign output parameters to component...
            if (Pts != null)
      {
        if (GH_Format.TreatAsCollection(Pts))
        {
          IEnumerable __enum_Pts = (IEnumerable)(Pts);
          DA.SetDataList(0, __enum_Pts);
        }
        else
        {
          if (Pts is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(0, (Grasshopper.Kernel.Data.IGH_DataTree)(Pts));
          }
          else
          {
            //assign direct
            DA.SetData(0, Pts);
          }
        }
      }
      else
      {
        DA.SetData(0, null);
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