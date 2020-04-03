using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

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
  private void RunScript(int n, List<string> S, ref object A, ref object B, ref object C, ref object D, ref object E, ref object F)
  {
                                                                /*
   System.Linq examples

  the basic lambda synthax is <var> => <anonymous expression with var>

  basic Linq functions:

  Where(<var> => <filtering condition with var>)
  Select(<var> => <expression with var>)
  Sum(<var> => <expression with var>)

   */

    int[] array = new int[n];
    int[] array2 = new int[array.Length];



    List<int> aList = new List<int>();

    // filling the array and list with the index itself
    for(int i = 0; i < array.Length; i++)
    {
      array[i] = i;
      aList.Add(i);
    }

    // filling the array and list with the index itself - the Linq way
    array = array.Select(i => i).ToArray();

    // generating an array of 1s
    array2 = array.Select(i => 1).ToArray();
    //aList = array.Select(i => 1).ToList(); // remember conversions ToArray and ToList

    // . . . . extracting only even numbers

    // extended synthax
    //A = from i in array where(i % 2 == 0) select(i);

    // lambda synthax
    A = array.Where(i => i % 2 == 0);

    // . . . . sum of all numbers
    B = array.Sum(i => i);

    // . . . . sum of all odd numbers
    C = array.Where(i => i % 2 != 0).Sum(i => i);

    // . . . . use in a function
    D = ConvertToGHInt(aList);

    E = ToDataTree(ConvertStringToInt(S));

    F = array.Where(i => i > n*0.5).FirstOrDefault();

  }

  // <Custom additional code> 
    public List<GH_Integer> ConvertToGHInt(List<int> list)
  {
    if (list == null) return null;
    else
      return list.Select(c => new GH_Integer(c)).ToList();
  }


  public int[][] ConvertStringToInt(List<String> Sc)
  {
    int[][] selCells = new int[Sc.Count][];

    for (int i = 0; i < Sc.Count; i++)
      selCells[i] = Sc[i].Split(',').Select(sd => Convert.ToInt32(sd)).ToArray();

    return selCells;
  }

  DataTree<GH_Integer> ToDataTree(int[][] array)
  {
    DataTree<GH_Integer> tOut = new DataTree<GH_Integer>();

    for(int i = 0; i < array.Length; i++)
      tOut.AddRange(array[i].Select(k => new GH_Integer(k)), new GH_Path(i));

    return tOut;
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
        int n = default(int);
    if (inputs[0] != null)
    {
      n = (int)(inputs[0]);
    }

    List<string> S = null;
    if (inputs[1] != null)
    {
      S = GH_DirtyCaster.CastToList<string>(inputs[1]);
    }


    //3. Declare output parameters
      object A = null;
  object B = null;
  object C = null;
  object D = null;
  object E = null;
  object F = null;


    //4. Invoke RunScript
    RunScript(n, S, ref A, ref B, ref C, ref D, ref E, ref F);
      
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
      if (D != null)
      {
        if (GH_Format.TreatAsCollection(D))
        {
          IEnumerable __enum_D = (IEnumerable)(D);
          DA.SetDataList(4, __enum_D);
        }
        else
        {
          if (D is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(4, (Grasshopper.Kernel.Data.IGH_DataTree)(D));
          }
          else
          {
            //assign direct
            DA.SetData(4, D);
          }
        }
      }
      else
      {
        DA.SetData(4, null);
      }
      if (E != null)
      {
        if (GH_Format.TreatAsCollection(E))
        {
          IEnumerable __enum_E = (IEnumerable)(E);
          DA.SetDataList(5, __enum_E);
        }
        else
        {
          if (E is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(5, (Grasshopper.Kernel.Data.IGH_DataTree)(E));
          }
          else
          {
            //assign direct
            DA.SetData(5, E);
          }
        }
      }
      else
      {
        DA.SetData(5, null);
      }
      if (F != null)
      {
        if (GH_Format.TreatAsCollection(F))
        {
          IEnumerable __enum_F = (IEnumerable)(F);
          DA.SetDataList(6, __enum_F);
        }
        else
        {
          if (F is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(6, (Grasshopper.Kernel.Data.IGH_DataTree)(F));
          }
          else
          {
            //assign direct
            DA.SetData(6, F);
          }
        }
      }
      else
      {
        DA.SetData(6, null);
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