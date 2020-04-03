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
  private void RunScript(List<Point3d> P, List<Point3d> P0, double radius, bool UseRTRee, ref object A, ref object B)
  {
        if (P == null || P.Count == 0) return;

    pts = P;
    cl.Clear();
    ci.Clear();

    if (UseRTRee)
    {

      // declare RTree
      RTree tree = new RTree();

      // populate it
      for(int i = 0; i < P.Count; i++)
        tree.Insert(P[i], i);

      // perform search
      
      // declare temporary lists for points and indexes
      List < Point3d > closest = new List<Point3d>();
      List <int> closestInd = new List<int>();

      // option 1.1: EventHandler declared on the fly
      EventHandler<RTreeEventArgs> rTreeCallback = (object sender, RTreeEventArgs e) =>
        {
        closest.Add(pts[e.Id]);
        closestInd.Add(e.Id);
        };

      for (int i = 0; i < P0.Count; i++)
      {
        closest = new List<Point3d>();
        closestInd = new List<int>();
        
        // option 1.1 & 1.2: call to external function
        tree.Search(new Sphere(P0[i], radius), rTreeCallback);
        
        //    option 2: use embedded anonymous function with lambda syntax
        //    tree.Search(new Sphere(P0, radius), (object sender, RTreeEventArgs e) =>
        //      {
        //      cl.Add(pts[e.Id]);
        //      ci.Add(e.Id);
        //      });
        
        // add lists to data trees
        cl.AddRange(closest, new GH_Path(i));
        ci.AddRange(closestInd, new GH_Path(i));
      }
    }
    else
    {
      for (int i = 0; i < P0.Count; i++)
        for (int j = 0; j < pts.Count; j++)
          if (pts[j].DistanceTo(P0[i]) < radius)
          {
            cl.Add(pts[j], new GH_Path(i));
            ci.Add(j, new GH_Path(i));
          }
    }

    A = cl;
    B = ci;

  }

  // <Custom additional code> 
    DataTree<Point3d> cl = new DataTree<Point3d>();
  DataTree<int> ci = new DataTree<int>();
  List<Point3d> pts = new List<Point3d>();

  // option 1.2: declare callback function as an external one
  //  private void rTreeCallback(object sender, RTreeEventArgs e){
  //    cl.Add(pts[e.Id]);
  //    ci.Add(e.Id);
  //  }

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
        List<Point3d> P = null;
    if (inputs[0] != null)
    {
      P = GH_DirtyCaster.CastToList<Point3d>(inputs[0]);
    }
    List<Point3d> P0 = null;
    if (inputs[1] != null)
    {
      P0 = GH_DirtyCaster.CastToList<Point3d>(inputs[1]);
    }
    double radius = default(double);
    if (inputs[2] != null)
    {
      radius = (double)(inputs[2]);
    }

    bool UseRTRee = default(bool);
    if (inputs[3] != null)
    {
      UseRTRee = (bool)(inputs[3]);
    }



    //3. Declare output parameters
      object A = null;
  object B = null;


    //4. Invoke RunScript
    RunScript(P, P0, radius, UseRTRee, ref A, ref B);
      
    try
    {
      //5. Assign output parameters to component...
            if (A != null)
      {
        if (GH_Format.TreatAsCollection(A))
        {
          IEnumerable __enum_A = (IEnumerable)(A);
          DA.SetDataList(1, __enum_A);
        }
        else
        {
          if (A is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(A));
          }
          else
          {
            //assign direct
            DA.SetData(1, A);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
      }
      if (B != null)
      {
        if (GH_Format.TreatAsCollection(B))
        {
          IEnumerable __enum_B = (IEnumerable)(B);
          DA.SetDataList(2, __enum_B);
        }
        else
        {
          if (B is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(2, (Grasshopper.Kernel.Data.IGH_DataTree)(B));
          }
          else
          {
            //assign direct
            DA.SetData(2, B);
          }
        }
      }
      else
      {
        DA.SetData(2, null);
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