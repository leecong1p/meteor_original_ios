﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MiniPose
{
    public int idx;
    public int start;
    public int end;
}

public class FMCPose
{
    public static Dictionary<string, FMCPose> fmcPose = new Dictionary<string, FMCPose>();
    public List<MiniPose> pos = new List<MiniPose>();
    public static FMCPose LoadPose(string file)
    {
        file += ".pos";
        if (fmcPose.ContainsKey(file))
            return fmcPose[file];
        FMCPose pose = new FMCPose();
        TextAsset asset = Resources.Load<TextAsset>(file);
        if (asset == null)
            return null;
        string text = System.Text.Encoding.ASCII.GetString(asset.bytes);
        string[] pos = text.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        int idx = 0;
        MiniPose po = null;
        for (int i = 0; i < pos.Length; i++)
        {
            string[] eachobj = pos[i].Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (eachobj[0] == "pose")
            {
                po = new MiniPose();
                po.idx = idx;
                pose.pos.Add(po);
                idx++;
            }
            if (eachobj[0] == "start")
            {
                if (po != null)
                    po.start = int.Parse(eachobj[1]);
            }
            if (eachobj[0] == "end")
            {
                if (po != null)
                    po.end = int.Parse(eachobj[1]);
            }
        }
        fmcPose.Add(file, pose);
        return pose;
    }
}
public class FMCPlayer : MonoBehaviour {
    public FMCFile frames;
    FMCPose pose;
    public string fmcFile;
    float f = 0;
    // Use this for initialization
    void Start () {
        if (frames != null)
            f = 1.0f / frames.Fps;
    }

    public void ChangeFrame(int fp)
    {
        currentFrame = fp;
    }

    bool looped = false;
    float tick;
    // Update is called once per frame
    void Update () {
        if (frames == null || state == 1)
            return;
        tick += Time.deltaTime;
        if (tick >= f)//不允许跳帧
        {
            currentFrame += 1;
            if (currentFrame >= end)
            {
                state = 3;
                if (looped)
                    currentFrame = start;
                else
                    currentFrame = end;
            }
            tick -= f;
        }

        int i = 0;
        foreach (var each in frames.frame[currentFrame].pos)
        {
            if (currentFrame == end)
                transform.GetChild(i).localPosition = each.Value;
            else
                transform.GetChild(i).localPosition = Vector3.Lerp(each.Value, frames.frame[currentFrame + 1].pos[each.Key], tick / f);
            i++;
        }

        i = 0;
        foreach (var each in frames.frame[currentFrame].quat)
        {
            if (currentFrame == end)
                transform.GetChild(i).localRotation = each.Value;
            else
                transform.GetChild(i).localRotation = Quaternion.Slerp(each.Value, frames.frame[currentFrame + 1].quat[each.Key], tick / f);
            i++;
        }
    }

    int start;
    int end;
    int currentFrame;
    int currentPosIdx;
    public int state;//1还未播放 2开始播放 3播放到最后一帧并停止
    public void ChangePose(int i, int loop)
    {
        if (pose != null && pose.pos.Count > i)
        {
            currentPosIdx = i;
            start = pose.pos[i].start;
            end = pose.pos[i].end;
            currentFrame = start;
            looped = (loop == 1);
            state = 2;
        }
    }

    public void Init(string file)
    {
        fmcFile = file;
        frames = FMCLoader.Instance.Load(fmcFile);
        pose = FMCPose.LoadPose(fmcFile);
        state = 1;
        //ChangePose(0, 0);
    }

    public int GetPose()
    {
        return currentPosIdx;
    }

    public int GetStatus()
    {
        return state;
    }
}
