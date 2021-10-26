using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class PARA
{
    public static int numIter = 2;
    public static float kStretch = 0.25f;
    public static float3 gravity = new float3(0.0f, -0.98f, 0.0f);
    public static float timeStep = 1.0f / 60.0f;
    public static float globalDamping = 0.98f;
    public static float3 ballCenter = new float3(0.0f, -4.0f, 3.0f);
    public static float ballRadius = 2.0f;
}
struct ClothData
{
    public float3 pos;
}
class DistanceConstraint
{
    public DistanceConstraint(int i1, int i2, float3 position1, float3 posiiton2, float k_)
    {
        index1 = i1;
        index2 = i2;

        pos1 = position1;
        pos2 = posiiton2;

        restLength = math.length(pos1 - pos2);

        k = k_;
        kPrime = 1.0f - math.pow((1.0f - k), 1.0f / PARA.numIter);
    }

    public int index1;
    public int index2;
    float3 pos1;
    float3 pos2;
    float k;
    public float kPrime;
    public float restLength;
}
class Cloth
{
    public float3[] nodePos;
    public float3[] nodeVel;
    public float3[] nodePredPos;
    public float3[] nodeForce;
    public float[] nodeMass;
    public float[] nodeInvMass;
    public List<DistanceConstraint> disConstraintList;
    ClothData[] dataForDraw;

    public int numNode;
    public int N;
    public Cloth(int N, float3 startPos, float nodeStep, float density)
    {

        // it shoule note that j + N * i = index
        this.N = N;
        numNode = N * N;

        // now init the nodePos
        nodePos = new float3[numNode];
        for (int index = 0; index < numNode; index++)
        {
            int i = index / N;
            int j = index - N * i;

            float3 tmpPos = new float3(i * nodeStep, 0.0f, j * nodeStep);
            nodePos[index] = tmpPos + startPos;
        }

        // now init the nodePredPos nodeFroce and nodeVel
        nodePredPos = new float3[numNode];
        nodeVel = new float3[numNode];
        nodeForce = new float3[numNode];
        for (int index = 0; index < numNode; index++)
        {
            nodePredPos[index] = new float3(0.0f, 0.0f, 0.0f);
            nodeVel[index] = new float3(0.0f, 0.0f, 0.0f);
            nodeForce[index] = new float3(0.0f, 0.0f, 0.0f);
        }

        //now init the nodeMass, there are (N-1) * (N-1) Trapeziums, and we deal with (N-1) * (N-1) * 2 triangles
        nodeMass = new float[numNode];
        nodeInvMass = new float[numNode];
        for (int i = 0; i < N - 1; i++)
        {
            for (int j = 0; j < N - 1; j++)
            {
                int index0 = j + i * N;
                int index1 = j + (i + 1) * N;
                int index2 = j + 1 + (i + 1) * N;
                int index3 = j + 1 + i * N;

                float tMass = nodeStep * nodeStep * 0.5f;
                nodeMass[index0] += tMass * 2.0f / 3.0f;
                nodeMass[index1] += tMass * 1.0f / 3.0f;
                nodeMass[index2] += tMass * 2.0f / 3.0f;
                nodeMass[index3] += tMass * 1.0f / 3.0f;
            }
        }

        for (int index = 0; index < numNode; index++)
        {
            nodeInvMass[index] = 1.0f / nodeMass[index];
        }
        // fix two points, so the mass should be inf 
        nodeInvMass[0] = 0.0f;
        nodeInvMass[N - 1] = 0.0f;

        // now we add distance constraints
        disConstraintList = new List<DistanceConstraint>();
        for (int i = 0; i < N - 1; i++)
        {
            for (int j = 0; j < N - 1; j++)
            {
                int index0 = j + i * N;
                int index1 = j + (i + 1) * N;
                int index2 = j + 1 + (i + 1) * N;
                int index3 = j + 1 + i * N;

                var tDisConstraint0 = new DistanceConstraint(index0, index1, nodePos[index0], nodePos[index1], PARA.kStretch);
                var tDisConstraint1 = new DistanceConstraint(index3, index2, nodePos[index3], nodePos[index2], PARA.kStretch);
                var tDisConstraint2 = new DistanceConstraint(index1, index2, nodePos[index1], nodePos[index2], PARA.kStretch);
                var tDisConstraint3 = new DistanceConstraint(index0, index3, nodePos[index0], nodePos[index3], PARA.kStretch);
                var tDisConstraint4 = new DistanceConstraint(index0, index2, nodePos[index0], nodePos[index2], PARA.kStretch);
                var tDisConstraint5 = new DistanceConstraint(index1, index3, nodePos[index1], nodePos[index3], PARA.kStretch);

                disConstraintList.Add(tDisConstraint0);
                disConstraintList.Add(tDisConstraint1);
                disConstraintList.Add(tDisConstraint2);
                disConstraintList.Add(tDisConstraint3);
                disConstraintList.Add(tDisConstraint4);
                disConstraintList.Add(tDisConstraint5);
            }
        }

        // now init the size of cloth data
        dataForDraw = new ClothData[(N - 1) * (N - 1) * 12];
    }

    public ClothData[] getColthDrawData()
    {
        for (int i = 0; i < N - 1; i++)
        {
            for (int j = 0; j < N - 1; j++)
            {
                int index = j + i * (N - 1);

                int index0 = j + i * N;
                int index1 = j + (i + 1) * N;
                int index2 = j + 1 + (i + 1) * N;
                int index3 = j + 1 + i * N;

                dataForDraw[index * 12 + 0].pos = nodePos[index0];
                dataForDraw[index * 12 + 1].pos = nodePos[index1];

                dataForDraw[index * 12 + 2].pos = nodePos[index1];
                dataForDraw[index * 12 + 3].pos = nodePos[index2];

                dataForDraw[index * 12 + 4].pos = nodePos[index2];
                dataForDraw[index * 12 + 5].pos = nodePos[index3];

                dataForDraw[index * 12 + 6].pos = nodePos[index3];
                dataForDraw[index * 12 + 7].pos = nodePos[index0];

                dataForDraw[index * 12 + 8].pos = nodePos[index0];
                dataForDraw[index * 12 + 9].pos = nodePos[index2];

                dataForDraw[index * 12 + 10].pos = nodePos[index1];
                dataForDraw[index * 12 + 11].pos = nodePos[index3];
            }
        }
        return dataForDraw;
    }

    public void updateStep(float3 ballPos)
    {
        calculateGravity();
        for (int i = 0; i < PARA.numIter; i++)
        {
            updateConstraints();
        }
        collisionDetect(ballPos);
        integrate();
    }

    void calculateGravity()
    {
        for (int i = 0; i < numNode; i++)
        {
            nodeForce[i] = new float3(0.0f, 0.0f, 0.0f);

            if (nodeInvMass[i] > 0)
            {
                nodeForce[i] += PARA.gravity * nodeMass[i];
            }
        }

        for (int i = 0; i < numNode; i++)
        {
            nodeVel[i] *= PARA.globalDamping;
            nodeVel[i] = nodeVel[i] + (nodeForce[i] * nodeInvMass[i] * PARA.timeStep);
        }

        for (int i = 0; i < numNode; i++)
        {
            if (nodeInvMass[i] <= 0.0f)
            {
                nodePredPos[i] = nodePos[i];
            }
            else
            {
                nodePredPos[i] = nodePos[i] + (nodeVel[i] * PARA.timeStep);
            }
        }
    }

    void updateConstraints()
    {
        foreach (DistanceConstraint tConstraint in disConstraintList)
        {
            int index1 = tConstraint.index1;
            int index2 = tConstraint.index2;

            float3 dirVec = nodePredPos[index1] - nodePredPos[index2];
            float len = math.length(dirVec);

            float w1 = nodeInvMass[index1];
            float w2 = nodeInvMass[index2];

            float3 dP = (1.0f / (w1 + w2)) * (len - tConstraint.restLength) * (dirVec / len) * tConstraint.kPrime;

            if (w1 > 0.0f)
                nodePredPos[index1] -= dP * w1;
            if (w2 > 0.0f)
                nodePredPos[index2] += dP * w2;
        }
    }

    void collisionDetect(float3 ballPos)
    {
        for (int i = 0; i < numNode; i++)
        {
            float3 gapVec = nodePredPos[i] - ballPos;
            float gapLen = math.length(gapVec);

            if (gapLen < PARA.ballRadius)
            {
                nodePredPos[i] += math.normalize(gapVec) * (PARA.ballRadius - gapLen);
                nodePos[i] = nodePredPos[i];
            }
        }
    }
    void integrate()
    {
        for (int i = 0; i < numNode; i++)
        {
            nodeVel[i] = (nodePredPos[i] - nodePos[i]) / PARA.timeStep;
            nodePos[i] = nodePredPos[i];
        }
    }
}
public class dispathcer : MonoBehaviour
{
    // Start is called before the first frame update
    int N;
    bool flag = false;

    ClothData[] dataForDraw; // (N-1) * (N-1) * 12
    ComputeBuffer cBufferDataForDraw;
    Cloth cloth;
    GameObject sphere;

    public Material mainMaterial;
    void Start()
    {
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.parent = this.transform;
        sphere.transform.position = new Vector3(PARA.ballCenter.x, PARA.ballCenter.y, PARA.ballCenter.z);
        sphere.transform.localScale = new Vector3(PARA.ballRadius * 2.0f, PARA.ballRadius * 2.0f, PARA.ballRadius * 2.0f);

        cloth = new Cloth(32, new float3(0.0f, 0.0f, 0.0f), 0.2f, 1.0f);

        N = cloth.N;

        dataForDraw = new ClothData[(N - 1) * (N - 1) * 12];
        dataForDraw = cloth.getColthDrawData();

        cBufferDataForDraw = new ComputeBuffer((N - 1) * (N - 1) * 12, 12);
        cBufferDataForDraw.SetData(dataForDraw, 0, 0, (N - 1) * (N - 1) * 12);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            flag = true;
        }
        if (flag)
        {
            //flag = false;
            cloth.updateStep(sphere.transform.position);
            dataForDraw = cloth.getColthDrawData();
            cBufferDataForDraw.SetData(dataForDraw, 0, 0, (N - 1) * (N - 1) * 12);
        }
    }
    private void OnRenderObject()
    {
        mainMaterial.SetBuffer("_clothDataBuffer", cBufferDataForDraw);
        mainMaterial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Lines, (cloth.N - 1) * (cloth.N - 1) * 12);
    }

    private void OnDestroy()
    {
        if (cBufferDataForDraw != null)
            cBufferDataForDraw.Release();
    }
}
