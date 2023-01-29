﻿using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Telesto.Interop;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Telesto
{

    internal abstract class Doodle
    {

        internal class Coordinate
        {

            internal enum CoordinateTypeEnum
            {
                Screen,
                World,
                Entity,
                Doodle,
                Waymark
            }

            internal Doodle doo { get; set; }
            internal CoordinateTypeEnum ct { get; set; }
            internal Vector3 cp { get; set; }

            internal string X { get; set; }
            internal string Y { get; set; }
            internal string Z { get; set; }
            internal ulong id { get; set; }
            internal string name { get; set; }

            internal Vector3 UnadjustedPosition(Plugin p)
            {
                switch (ct)
                {
                    default:
                    case CoordinateTypeEnum.Screen:
                        return new Vector3(
                            (float)p.EvaluateNumericExpression(doo, X),
                            (float)p.EvaluateNumericExpression(doo, Y),
                            (float)p.EvaluateNumericExpression(doo, Z)
                        );
                        break;
                    case CoordinateTypeEnum.World:
                        return new Vector3(
                            (float)p.EvaluateNumericExpression(doo, X),
                            (float)p.EvaluateNumericExpression(doo, Y),
                            (float)p.EvaluateNumericExpression(doo, Z)
                        );
                        break;
                    case CoordinateTypeEnum.Entity:
                        GameObject go;
                        if (id > 0)
                        {
                            go = p.GetEntityById(id);
                        }
                        else
                        {
                            go = p.GetEntityByName(name);
                        }
                        if (go != null)
                        {
                            return new Vector3(
                                go.Position.X,
                                go.Position.Y,
                                go.Position.Z
                            );
                        }
                        else
                        {
                            return new Vector3(
                                (float)p.EvaluateNumericExpression(doo, X),
                                (float)p.EvaluateNumericExpression(doo, Y),
                                (float)p.EvaluateNumericExpression(doo, Z)
                            );
                        }
                        break;
                    case CoordinateTypeEnum.Doodle:
                        string[] spl = name.Split("/");
                        Doodle d = p.GetDoodleByName(spl[0]);
                        if (d != null)
                        {
                            Coordinate c = d.GetCoordinateByName(spl.Length > 1 ? spl[1] : "");
                            if (c != null)
                            {
                                return c.UnadjustedPosition(p);
                            }
                        }
                        return new Vector3();
                    case CoordinateTypeEnum.Waymark:
                        Waymark wm = p.GetWaymarkByName(name);
                        if (wm != null && wm.Active == true)
                        {
                            return new Vector3(
                                wm.X_Float,
                                wm.Y_Float,
                                wm.Z_Float
                            );
                        }
                        return new Vector3();
                }
            }

            internal void RefreshVector(Plugin p)
            {
                switch (ct)
                {
                    case CoordinateTypeEnum.Screen:
                        cp = new Vector3(
                            (float)p.EvaluateNumericExpression(doo, X),
                            (float)p.EvaluateNumericExpression(doo, Y),
                            (float)p.EvaluateNumericExpression(doo, Z)
                        );
                        break;
                    case CoordinateTypeEnum.World:
                        cp = p.TranslateToScreen(
                            p.EvaluateNumericExpression(doo, X),
                            p.EvaluateNumericExpression(doo, Y),
                            p.EvaluateNumericExpression(doo, Z)
                        );
                        break;
                    case CoordinateTypeEnum.Entity:
                        GameObject go;
                        if (id > 0)
                        {
                            go = p.GetEntityById(id);
                        }
                        else
                        {
                            go = p.GetEntityByName(name);
                        }
                        if (go != null)
                        {
                            cp = p.TranslateToScreen(
                                go.Position.X,
                                go.Position.Y,
                                go.Position.Z
                            );
                        }
                        else
                        {
                            cp = p.TranslateToScreen(
                                p.EvaluateNumericExpression(doo, X),
                                p.EvaluateNumericExpression(doo, Y),
                                p.EvaluateNumericExpression(doo, Z)
                            );
                        }
                        break;
                    case CoordinateTypeEnum.Doodle:
                        string[] spl = name.Split("/");
                        Doodle d = p.GetDoodleByName(spl[0]);
                        if (d != null)
                        {
                            Coordinate c = d.GetCoordinateByName(spl.Length > 1 ? spl[1] : "");
                            if (c != null)
                            {
                                Vector3 uap = c.UnadjustedPosition(p);
                                cp = p.TranslateToScreen(
                                    uap.X,
                                    uap.Y,
                                    uap.Z
                                );
                            }
                            else
                            {
                                cp = new Vector3();
                            }
                        }
                        else
                        {
                            cp = new Vector3();
                        }
                        break;
                    case CoordinateTypeEnum.Waymark:                        
                        Waymark wm = p.GetWaymarkByName(name);
                        if (wm != null && wm.Active == true)
                        {
                            cp = p.TranslateToScreen(
                                wm.X_Float,
                                wm.Y_Float,
                                wm.Z_Float
                            );
                        }
                        else
                        {
                            cp = new Vector3();
                        }
                        break;
                }
            }

            internal void Initialize(Dictionary<string, object> d)
            {                
                string coords = (d.ContainsKey("coords") == true) ? d["coords"].ToString() : "screen";
                switch (coords)
                {
                    case "world":
                        ct = CoordinateTypeEnum.World;
                        break;
                    case "entity":
                        ct = CoordinateTypeEnum.Entity;
                        break;
                    case "doodle":
                        ct = CoordinateTypeEnum.Doodle;
                        break;
                    case "waymark":
                        ct = CoordinateTypeEnum.Waymark;
                        break;
                    default:
                        ct = CoordinateTypeEnum.Screen;
                        break;
                }
                X = (d.ContainsKey("x") == true) ? d["x"].ToString() : "0";
                Y = (d.ContainsKey("y") == true) ? d["y"].ToString() : "0";
                Z = (d.ContainsKey("z") == true) ? d["z"].ToString() : "0";
                id = 0;
                if (d.ContainsKey("id") == true)
                {
                    ulong honk;
                    if (ulong.TryParse(d["id"].ToString(), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out honk) == true)
                    {
                        id = honk;
                    }
                }
                if (id == 0)
                {
                    name = (d.ContainsKey("name") == true) ? d["name"].ToString() : "";
                }
            }

        }

        [Flags]
        internal enum ExpiryTypeEnum
        {
            Timed = 0x01,
            OnDeath = 0x02,
            OnWipe = 0x04,
            OnZoneChange = 0x08,
            NotLoggedIn = 0x10
        }

        internal Plugin p { get; set; }
        internal string Name { get; set; }
        internal DateTime Created { get; set; } = DateTime.UtcNow;
        internal DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddSeconds(5);
        internal ExpiryTypeEnum ExpiresOn { get; set; } = ExpiryTypeEnum.Timed | ExpiryTypeEnum.NotLoggedIn | ExpiryTypeEnum.OnZoneChange | ExpiryTypeEnum.OnWipe;
        internal Vector4 col { get; set; } = new Vector4();

        internal string R { get; set; }
        internal string G { get; set; }
        internal string B { get; set; }
        internal string A { get; set; }

        abstract internal Coordinate GetCoordinateByName(string id);

        internal virtual void Initialize(Dictionary<string, object> d)
        {
            Name = d["name"].ToString();
            R = (d.ContainsKey("r") == true) ? d["r"].ToString() : "0";
            G = (d.ContainsKey("g") == true) ? d["g"].ToString() : "0";
            B = (d.ContainsKey("b") == true) ? d["b"].ToString() : "0";
            A = (d.ContainsKey("a") == true) ? d["a"].ToString() : "1";
            if (d.ContainsKey("expireson") == true)
            {
                ExpiryTypeEnum nt = 0;
                string[] vals = d["expireson"].ToString().Split(",", StringSplitOptions.TrimEntries);
                foreach (string val in vals)
                {
                    switch (val.ToLower())
                    {
                        case "timed": nt |= ExpiryTypeEnum.Timed; break;
                        case "ondeath": nt |= ExpiryTypeEnum.OnDeath; break;
                        case "onwipe": nt |= ExpiryTypeEnum.OnWipe; break;
                        case "onzonechange": nt |= ExpiryTypeEnum.OnZoneChange; break;
                        case "notloggedin": nt |= ExpiryTypeEnum.NotLoggedIn; break;
                    }
                }
                ExpiresOn = nt;
            }
            if (d.ContainsKey("expiresin") == true)
            {
                int ms = Int32.Parse(d["expiresin"].ToString());
                ExpiresAt = DateTime.UtcNow.AddMilliseconds(ms);
                ExpiresOn |= ExpiryTypeEnum.Timed;
            }
        }

        internal static Doodle Deserialize(Dictionary<string, object> d)
        {
            string type = d["type"].ToString();
            Doodle doo = null;
            switch (type.ToLower())
            {
                case "line":
                    doo = new Doodles.Line();
                    break;
                case "text":
                    doo = new Doodles.Text();
                    break;
                case "circle":
                    doo = new Doodles.Circle();
                    break;
                case "rectangle":
                    doo = new Doodles.Rectangle();
                    break;
                case "waymark":
                    doo = new Doodles.Waymark();
                    break;
                case "arrow":
                    doo = new Doodles.Arrow();
                    break;
                case "beam":
                    doo = new Doodles.Beam();
                    break;
            }
            if (doo != null)
            {
                doo.Initialize(d);
            }
            return doo;
        }

        internal virtual bool Update()
        {
            if ((ExpiresOn & ExpiryTypeEnum.Timed) != 0)
            {
                if (DateTime.UtcNow >= ExpiresAt)
                {
                    return false;
                }
            }
            col = new Vector4(
                (float)p.EvaluateNumericExpression(this, R),
                (float)p.EvaluateNumericExpression(this, G),
                (float)p.EvaluateNumericExpression(this, B),
                (float)p.EvaluateNumericExpression(this, A)
            );
            return true;
        }

        internal abstract void Draw();

    }

}
