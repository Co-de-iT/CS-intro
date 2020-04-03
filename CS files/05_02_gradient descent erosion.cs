using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino.Collections;

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
  private void RunScript(List<Point3d> P, Mesh M, double step, double eInd, double eMax, bool reset, bool go, ref object Me, ref object Pos, ref object Trails)
  {
    
    if (reset || er == null)
    {
      er = new ErosionSim(M, P, step);
      pts = new Point3d[P.Count];
      trs = new Polyline[P.Count];

    }


    if (go)
    {
      // updates our Particles (only if they are alive)
      er.maxSpeed = step;
      er.erosionIndex = eInd;
      er.erosionMax = eMax;

      er.update();
      //foreach(Particle a in parts) if (a.alive) a.update(M1, step, eInd, eMax);

      // expiring solution forces the component to update
      Component.ExpireSolution(true);
    }

    // extracts positions and trails
    for(int i = 0; i < er.parts.Count; i++)
    {
      pts[i] = er.parts[i].pos;
      trs[i] = er.parts[i].trail.IsValid ? er.parts[i].trail : null;
    }

    Pos = pts;
    Trails = trs;
    Me = er.extractMesh();
  }

  // <Custom additional code> 
  
  // persistent variables
  Point3d[] pts;
  Polyline[] trs;
  ErosionSim er;

  public class ErosionSim
  {
    public List<Particle> parts;
    public double erosionIndex;
    public double erosionMax;
    public Point3dList p1;
    public Mesh M;
    public Mesh Morig;
    public double maxSpeed;

    public ErosionSim(Mesh M, List<Point3d> pts, double maxSpeed)
    {
      this.M = M;
      Morig = new Mesh();
      Morig.CopyFrom(M);
      this.maxSpeed = maxSpeed;
      p1 = new Point3dList(M.Vertices.ToPoint3dArray());
      initParts(pts);
    }

    // initialization function
    public void initParts(List<Point3d> P)
    {
      // clears lists
      parts = new List<Particle>();

      // initializes lists
      foreach(Point3d p in P) parts.Add(new Particle(this, p));
    }

    public void update()
    {
      foreach(Particle pa in parts)
        if (pa.alive) pa.update();

      for(int i = 0; i < p1.Count; i++)
        M.Vertices.SetVertex(i, p1[i]);

      M.RebuildNormals();
    }

    public Mesh extractMesh()
    {
      return M;
    }

  }



  // Particle class

  public class Particle // in this case public is unnecessary but in the general case it is
  {

    // fields
    public Point3d pos;
    public Vector3d vel;
    public Polyline trail;
    public bool alive;
    public ErosionSim er;

    // constructor (public if we use it from RunScript)
    public Particle(ErosionSim er, Point3d pos){
      this.er = er;
      this.pos = pos;
      vel = Vector3d.Zero;
      trail = new Polyline();
      trail.Add(pos);
      alive = true;
    }

    // methods

    public void update()
    {
      calcVel();
      erode2();
      move();
    }

    void calcVel()
    {
      // get closest point to the Mesh
      MeshPoint mP = er.M.ClosestMeshPoint(pos, 0.0);
      // evaluate Mesh normal at that point
      Vector3d mN = er.M.NormalAt(mP);
      // find tangent to mesh (perpendicular vector to normal)
      vel = Vector3d.CrossProduct(mN, Vector3d.ZAxis);
      // rotate 90 degrees
      vel.Rotate(Math.PI * 0.5, mN);
      // unitize
      vel.Unitize();
      // set to max speed (step)
      vel *= er.maxSpeed;
    }

    void erode()
    {
      int vi = er.p1.ClosestIndex(pos);
      if(er.M.Vertices[vi].DistanceTo(er.Morig.Vertices[vi]) < er.erosionMax)
      {
        Vector3d disp = -1 * (Vector3d) er.M.Normals[vi] * er.erosionIndex;
        er.p1[vi] += disp;
      }
    }
    

    void erode2()
    {
      int vi = er.p1.ClosestIndex(pos);
      int[] vNeigh = er.M.Vertices.GetConnectedVertices(vi);
      if(er.M.Vertices[vi].DistanceTo(er.Morig.Vertices[vi]) < er.erosionMax)
      {
        Vector3d disp = -1 * (Vector3d) er.M.Normals[vi] * er.erosionIndex;
        er.p1[vi] += disp;
        for(int i=0; i< vNeigh.Length; i++)
          er.p1[vNeigh[i]] += disp * 0.25;
      }
    }


    void move()
    {
      // calculates new position
      Point3d newPos = pos + vel;
      newPos = er.M.ClosestPoint(newPos);

      // kills particle if it has reached a pit - update if not
      if (Math.Abs(newPos.Z - pos.Z) < 0.01 || pos.DistanceToSquared(newPos) < er.maxSpeed*0.2) alive = false;
      else
      {
        pos = newPos;
        trail.Add(pos);
      }
    }

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
        List<Point3d> P = null;
    if (inputs[0] != null)
    {
      P = GH_DirtyCaster.CastToList<Point3d>(inputs[0]);
    }
    Mesh M = default(Mesh);
    if (inputs[1] != null)
    {
      M = (Mesh)(inputs[1]);
    }

    double step = default(double);
    if (inputs[2] != null)
    {
      step = (double)(inputs[2]);
    }

    double eInd = default(double);
    if (inputs[3] != null)
    {
      eInd = (double)(inputs[3]);
    }

    double eMax = default(double);
    if (inputs[4] != null)
    {
      eMax = (double)(inputs[4]);
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



    //3. Declare output parameters
      object Me = null;
  object Pos = null;
  object Trails = null;


    //4. Invoke RunScript
    RunScript(P, M, step, eInd, eMax, reset, go, ref Me, ref Pos, ref Trails);
      
    try
    {
      //5. Assign output parameters to component...
            if (Me != null)
      {
        if (GH_Format.TreatAsCollection(Me))
        {
          IEnumerable __enum_Me = (IEnumerable)(Me);
          DA.SetDataList(1, __enum_Me);
        }
        else
        {
          if (Me is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(Me));
          }
          else
          {
            //assign direct
            DA.SetData(1, Me);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
      }
      if (Pos != null)
      {
        if (GH_Format.TreatAsCollection(Pos))
        {
          IEnumerable __enum_Pos = (IEnumerable)(Pos);
          DA.SetDataList(2, __enum_Pos);
        }
        else
        {
          if (Pos is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(2, (Grasshopper.Kernel.Data.IGH_DataTree)(Pos));
          }
          else
          {
            //assign direct
            DA.SetData(2, Pos);
          }
        }
      }
      else
      {
        DA.SetData(2, null);
      }
      if (Trails != null)
      {
        if (GH_Format.TreatAsCollection(Trails))
        {
          IEnumerable __enum_Trails = (IEnumerable)(Trails);
          DA.SetDataList(3, __enum_Trails);
        }
        else
        {
          if (Trails is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(3, (Grasshopper.Kernel.Data.IGH_DataTree)(Trails));
          }
          else
          {
            //assign direct
            DA.SetData(3, Trails);
          }
        }
      }
      else
      {
        DA.SetData(3, null);
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