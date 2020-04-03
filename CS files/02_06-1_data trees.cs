using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



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
  private void RunScript(ref object A, ref object B, ref object C, ref object __, ref object D, ref object E, ref object F, ref object G)
  {
        // declare a DataTree
    DataTree <Point3d> dT = new DataTree<Point3d>();

    // add data to a DataTree
    for (int i = 0; i < 10; i++)
    {
      Point3d pt = new Point3d(i, 0, 0);

      // Adding data to a DataTree requires a path.
      // In this case the path will have a single branch index.
      // There will be only one Point associated with each path
      GH_Path Path = new GH_Path(0, i);

      // adding single elements to the DataTree
      dT.Add(pt, Path);
    }

    A = dT;

    // ..........................................................................................................

    DataTree <Point3d> dT1 = new DataTree<Point3d>();
    // in case there is no path specified, data is stored in default branch {0}
    dT1.Add(new Point3d(0.0, 1.0, 0));

    // any overload that accepts args allows us to declare a row of comma separated values
    // such as ---> new GH_Path(Int32[] args)
    GH_Path myPath = new GH_Path(1, 2, 3);

    dT1.Add(new Point3d(1.0, 1.0, 0), myPath);

    int[] pNums = {1,2,4};
    myPath = new GH_Path(pNums);

    dT1.Add(new Point3d(2.0, 1.0, 0), myPath);


    B = dT1;

    // ..........................................................................................................

    DataTree <Point3d> dT2 = new DataTree<Point3d>();

    // add data to the DataTree
    for (int i = 0; i < 10; i++)
      for(int j = 0; j < 10; j++)
        for(int k = 0; k < 10; k++)
        {
          Point3d pt = new Point3d(i, j + 2.0, k);


          // this time the path will have 2 dimensions: rows and columns
          GH_Path Path = new GH_Path(0, i, j);

          // adding single elements to the DataTree
          dT2.Add(pt, Path);
        }


    C = dT2;

    // retrieving a branch
    //
    // data in DataTrees can be retried with the following syntaxes
    // DataTree.Branch(path)
    // DataTree.Branch([]args)
    // DataTree.Branch(sequential branch index)
    D = dT2.Branch(0, 2, 3);

    // retrieving an element
    // same syntax as above, add element index between []
    E = dT2.Branch(0, 2, 3)[3];

    // branch count and list count
    F = dT2.BranchCount;     // or dT2.Branches.Count;
    G = dT2.Branch(0, 2, 3).Count;

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
    

    //3. Declare output parameters
      object A = null;
  object B = null;
  object C = null;
  object __ = null;
  object D = null;
  object E = null;
  object F = null;
  object G = null;


    //4. Invoke RunScript
    RunScript(ref A, ref B, ref C, ref __, ref D, ref E, ref F, ref G);
      
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
      if (C != null)
      {
        if (GH_Format.TreatAsCollection(C))
        {
          IEnumerable __enum_C = (IEnumerable)(C);
          DA.SetDataList(3, __enum_C);
        }
        else
        {
          if (C is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(3, (Grasshopper.Kernel.Data.IGH_DataTree)(C));
          }
          else
          {
            //assign direct
            DA.SetData(3, C);
          }
        }
      }
      else
      {
        DA.SetData(3, null);
      }
      if (__ != null)
      {
        if (GH_Format.TreatAsCollection(__))
        {
          IEnumerable __enum___ = (IEnumerable)(__);
          DA.SetDataList(4, __enum___);
        }
        else
        {
          if (__ is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(4, (Grasshopper.Kernel.Data.IGH_DataTree)(__));
          }
          else
          {
            //assign direct
            DA.SetData(4, __);
          }
        }
      }
      else
      {
        DA.SetData(4, null);
      }
      if (D != null)
      {
        if (GH_Format.TreatAsCollection(D))
        {
          IEnumerable __enum_D = (IEnumerable)(D);
          DA.SetDataList(5, __enum_D);
        }
        else
        {
          if (D is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(5, (Grasshopper.Kernel.Data.IGH_DataTree)(D));
          }
          else
          {
            //assign direct
            DA.SetData(5, D);
          }
        }
      }
      else
      {
        DA.SetData(5, null);
      }
      if (E != null)
      {
        if (GH_Format.TreatAsCollection(E))
        {
          IEnumerable __enum_E = (IEnumerable)(E);
          DA.SetDataList(6, __enum_E);
        }
        else
        {
          if (E is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(6, (Grasshopper.Kernel.Data.IGH_DataTree)(E));
          }
          else
          {
            //assign direct
            DA.SetData(6, E);
          }
        }
      }
      else
      {
        DA.SetData(6, null);
      }
      if (F != null)
      {
        if (GH_Format.TreatAsCollection(F))
        {
          IEnumerable __enum_F = (IEnumerable)(F);
          DA.SetDataList(7, __enum_F);
        }
        else
        {
          if (F is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(7, (Grasshopper.Kernel.Data.IGH_DataTree)(F));
          }
          else
          {
            //assign direct
            DA.SetData(7, F);
          }
        }
      }
      else
      {
        DA.SetData(7, null);
      }
      if (G != null)
      {
        if (GH_Format.TreatAsCollection(G))
        {
          IEnumerable __enum_G = (IEnumerable)(G);
          DA.SetDataList(8, __enum_G);
        }
        else
        {
          if (G is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(8, (Grasshopper.Kernel.Data.IGH_DataTree)(G));
          }
          else
          {
            //assign direct
            DA.SetData(8, G);
          }
        }
      }
      else
      {
        DA.SetData(8, null);
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