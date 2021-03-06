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
  private void RunScript(double x, double y, ref object A)
  {
        // dealing with delegates - or how to pass functions as parameters to other functions

    // ----> start from the Custom additional code below

    // continue from here when done:

    // the Foo function can be called passing as parameter directly the name of
    // any function compatible with the delegate signature:
    Foo(ComputeAverage, x, y); // this will pass the ComputeAverage function as parameter
    Foo(ComputeSquaredSum, x, y); // this will pass the ComputeSquaredSum function as parameter

    // another option is to define instances of BynaryOperator (remember, it's a data type)
    BinaryOperator myFavoriteBinaryOperator = ComputeAverage;
    BinaryOperator yourFavoriteBinaryOperator = ComputeSquaredSum;
    // ... and then pass these to the Foo funciton as parameters
    Foo(myFavoriteBinaryOperator, x, y);
    Foo(yourFavoriteBinaryOperator, x, y);

    // I can also use an anonymous function as a parameter for the delegate
    // provided that it is compatible with the delegate signature:
    Foo((a, b) => {return a + b;}, x, y);
    // C# automatically assumes that a,b are doubles but the function body
    // will have to return a double as well
    // see explanation on labda expression and anonymous functions in this
    // GH definition

  }

  // <Custom additional code> 
    /*

  a delegate is a data type that allows me to pass functions as variables,
  a delegate defines a new data type that represents any function that
  has a matching signature with the delegate

  Let's see an example of delegate declaration:

  delegate <type> <name>(<parameters>);

  */

  delegate double BinaryOperator(double a, double b);

  /*
   BinaryOperator is now a new data type.
   Just as with classes, first I declare my delegate as a new data type,
   but if I need one I have to declare an instance, like:

 BynaryOperator myOp;

 now myOp can be any function that has the same signature of the delegate
 (in this case, one that accepts 2 doubles as parameters and returns a double)

 Here are 2 functions whose signature match the delegate BinaryOperator:
 */

  double ComputeAverage (double first, double second)
  {
    return (first + second) * 0.5;
  }

  double ComputeSquaredSum(double m, double n)
  {
    return (m + n) * (m + n);
  }


  // Finally, here's a function that has the delegate as a parameter:
  void Foo(BinaryOperator bOperator, double a, double b)
  {
    // this will store the result of the function that will be passed as delegate
    double result = bOperator(a, b);
    // and then print it
    Print(result.ToString());
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
        double x = default(double);
    if (inputs[0] != null)
    {
      x = (double)(inputs[0]);
    }

    double y = default(double);
    if (inputs[1] != null)
    {
      y = (double)(inputs[1]);
    }



    //3. Declare output parameters
      object A = null;


    //4. Invoke RunScript
    RunScript(x, y, ref A);
      
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