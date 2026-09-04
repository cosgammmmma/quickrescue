namespace URWPGSim2D.Strategy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using URWPGSim2D.Common;

    public class GlobalVar
    {
        public static double[,] abc = new double[0x10, 0x10];
        public static int Attack_Line = 500;
        public static int b0_l;
        public static int b0_r;
        public static int b1_l;
        public static int b1_r;
        public static int b2_l;
        public static int b2_r;
        public static int b3_l;
        public static int b3_r;
        public static int b4_l;
        public static int b4_r;
        public static int b5_l;
        public static int b5_r;
        public static int b6_l;
        public static int b6_r;
        public static int b7_l;
        public static int b7_r;
        public static int b8_l;
        public static int b8_r;
        public static BaseAction BaAction = new BaseAction();
        public static int back = 0;
        public static double[,] ball_locate = new double[,] { { -120.0, -120.0 }, { -120.0, 0.0 }, { -120.0, 120.0 }, { 0.0, -120.0 }, { 0.0, 0.0 }, { 0.0, 120.0 }, { 120.0, -120.0 }, { 120.0, 0.0 }, { 120.0, 120.0 } };
        public static Ball[] balls = new Ball[9];
        public static int[] boundary_dribbleact = new int[2];
        public static int[] Bringing_BallID = new int[] { -1, -1 };
        public static int buffle_flag = 0;
        public static int[] byPass_flag = new int[2];
        public static int[] chooseinterfereBallStage = new int[2];
        public static int[] chooseinterfereBallStage_Down = new int[2];
        public static int[] chooseinterfereBallStage_Up = new int[2];
        public static int[] Circle_last2min_tag = new int[2];
        public static int cnt = 0;
        public static int cnt1 = 0;
        public static int cnt2 = 0;
        public static int cntplus = 0;
        public static int[] corner = new int[2];
        public static int[] corner_help = new int[2];
        public static int[] count = new int[5];
        public static int CQstage = 0;
        public static int ctp = 0;
        public static int[] danyuchujie = new int[2];
        public static int[] danyujinqiu = new int[2];
        public static int[] dead = new int[2];
        public static int[] deadjudge = new int[2];
        public static int deadtime0 = 0;
        public static int deadtime1 = 0;
        public static Decision[] decisions = null;
        public static int Defence_Line = -500;
        public static int[] Delay = new int[5];
        public static double[,] differencial_x = new double[2, 5];
        public static double[,] differencial_z = new double[2, 5];
        public static Platform Down_Dead_ball_point = new Platform(1125.0, 750.0);
        public static int[] Dribble_boundaryf_zt1 = new int[] { 100, 100 };
        public static int[] Dribble_boundaryf_zt2 = new int[] { 100, 100 };
        public static int[] Dribbleboundary_zt1 = new int[2];
        public static int[] Dribbleboundary_zt2 = new int[2];
        public static int DStage = 0;
        public static double[] enemydirection0 = new double[0x19];
        public static double[] enemydirection1 = new double[0x19];
        public static double[] enemyx0 = new double[0x19];
        public static double[] enemyx1 = new double[0x19];
        public static double[] enemyz0 = new double[0x19];
        public static double[] enemyz1 = new double[0x19];
        public static int[] enter_gate_stage = new int[2];
        public static Platform error = new Platform(0.0, 0.0);
        public static double[,,] error_record = new double[9, 2, 1];
        public static int[,] errorflag = new int[9, 1];
        public static int escape_lanqiu = 0;
        public static int final = 0;
        public static int finallanqiu = 0;
        public static int Fins_width = 0x2d;
        public static int Fish_centertohead = 0x69;
        public static float Fish_length = 422.8f;
        public static int Fishbody_width = 0x2c;
        public static int Fishhead_Radius = 30;
        public static int flag_duqiumen = 0;
        public static int Forbidden_downline = 500;
        public static int Forbidden_length = 0x3e8;
        public static int Forbidden_line = -1700;
        public static int Forbidden_upline = -500;
        public static int Forbidden_width = 400;
        public static int GFY = 0;
        public static int Goal_downline = 200;
        public static int Goal_length = 400;
        public static int Goal_line = -2100;
        public static int Goal_upline = -200;
        public static int Goal_width = 150;
        public static int gotoleft = 1;
        public static int[] grab_flag = new int[2];
        public static int[] grab_flag_opp = new int[2];
        private static GlobalVar instance = null;
        public static int[] interfereBallStage = new int[2];
        public static int[] interfereBallStage_Down = new int[2];
        public static int[] interfereBallStage_Up = new int[2];
        public static bool[] is_ball_choosed = new bool[9];
        public static bool[] is_ZKs_rescure_end = new bool[2];
        public static int[] jiaoluobaiwei = new int[2];
        public static double[,] jimiao = new double[9, 5];
        public static int[] Jinqiu_Rescue = new int[2];
        public static int[] lanqiuFlag = new int[2];
        public static int[] last2min_is_in_200m = new int[2];
        public static int[] last2min_up_or_down = new int[] { 1, -1 };
        public static int[] leftScore = new int[10];
        public static int[] leftScoreCopy = new int[10];
        public static int[] liangqiutongchan = new int[2];
        public static int[] locked = new int[2];
        public static int locktime0 = 0;
        public static int locktime1 = 0;
        public static int[] meaningless = new int[2];
        public static Mission mission;
        public static List<Fish> myFish = new List<Fish>();
        public static string MyFlashDisk = "东北大学";
        public static Team<RoboFish> MyTeam = null;
        public static Team<RoboFish> MyTeamLast = null;
        public static int n = 0;
        public static Platform Opp_Down_Dead_ball_point = new Platform(-1125.0, 750.0);
        public static int Opp_Forbidden_downline = 500;
        public static int Opp_Forbidden_line = 0x6a4;
        public static int Opp_Forbidden_upline = -500;
        public static int Opp_Goal_downline = 200;
        public static int Opp_Goal_line = 0x834;
        public static int Opp_Goal_upline = -200;
        public static Platform OPP_Penalty_point = new Platform(-1750.0, 0.0);
        public static Platform Opp_Up_Dead_ball_point = new Platform(-1125.0, -750.0);
        public static Team<RoboFish> OppTeam = null;
        public static int oppTeamId;
        public static int Paishu = 0;
        public static Platform Penalty_point = new Platform(1750.0, 0.0);
        public static double PI = 3.1415926535897931;
        public static ArrayList po = new ArrayList();
        public static fish_mode[] prefish = new fish_mode[2];
        public static int PV = 1;
        public static int Radius = 0x3a;
        public static double[,] recordfishz = new double[,] { {
            500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0,
            500.0, 500.0, 500.0, 500.0
        }, {
            500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0, 500.0,
            500.0, 500.0, 500.0, 500.0
        } };
        public static int[] Rescue_boundary_tail = new int[2];
        public static int[] Rescue_boundary_Zhuangtai = new int[2];
        public static int[] Rescue_boundary_Zhuangtai1 = new int[2];
        public static int[] Rescue_boundary_zt1 = new int[2];
        public static int[] Rescue_boundary_zt2 = new int[2];
        public static int[] Rescue_boundaryact_sign = new int[2];
        public static int[] rightScore = new int[10];
        public static int[] rightScoreCopy = new int[10];
        public static int ScoreSum;
        public static int[] SingleBringing_BallID = new int[2];
        public static int Stage = 0;
        public static int stage_zh = 0;
        public static int Start;
        public static Platform Start_point = new Platform(0.0, 0.0);
        public static float tail_length = 64.8f;
        public static int tail_width = 0x69;
        public static int teamId;
        public static double[,] test_to_twist_ballx = new double[2, 5];
        public static double[,] test_to_twist_ballz = new double[2, 5];
        public static double[,] test_to_twist_x = new double[2, 5];
        public static double[,] test_to_twist_z = new double[2, 5];
        public static double[] tTable = new double[] { -0.3552, -0.2921, -0.22, -0.1731, -0.1235, -0.0784, -0.0469, 0.0, 0.0469, 0.0784, 0.1235, 0.1731, 0.22, 0.2921, 0.3438, 0.3438 };
        public static int UnarmedFish = 0;
        public static int UnarmedFlag0 = 0;
        public static int UnarmedFlag1 = 0;
        public static int[] UnarmedlastFlag = new int[2];
        public static int UnarmedStage = 0;
        public static Platform Up_Dead_ball_point = new Platform(1125.0, -750.0);
        public static int up_flag = 0;
        public static double[] vTable = new double[] { 0.0, 9.02, 31.55, 60.4, 88.35, 110.66, 132.78, 152.16, 172.9, 204.65, 268.52, 289.33, 295.66, 293.99, 303.69, 314.51 };
        public static int[] warrior = new int[2];
        public static int[] whether_change_to_test3 = new int[2];
        public static int[] willbring_ballid = new int[] { -1, -1 };
        public static int wy_emergency = 0;
        public static int wy_emergency_exe = 0;
        public static int Yard_halflength = 0x834;
        public static int Yard_halfwidth = 0x5dc;
        public static Balls[] yBall = new Balls[9];
        public static int[] youlockme = new int[2];
        public static int YStage = 0;
        public static int[] zhuangtaibaochi_danyu = new int[2];

        public static void Init()
        {
        }

        public static GlobalVar Instance()
        {
            if (instance == null)
            {
                instance = new GlobalVar();
            }
            return instance;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Balls
        {
            internal double X;
            internal double Z;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct fish_mode
        {
            public double prex;
            public double prez;
            public int preVcode;
            public int preTcode;
            public double predirection;
        }
    }
}

