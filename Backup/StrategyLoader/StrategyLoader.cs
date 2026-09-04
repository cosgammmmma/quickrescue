//-----------------------------------------------------------------------
// Copyright (C), 2010, PKU&HNIU
// File Name: StrategyLoader.cs
// Date: 20101120  Author: LiYoubing  Version: 1
// Description: 用于动态加载策略dll文件的策略接口定义文件
// Histroy:
// Date: 20101120  Author: LiYoubing
// Modification: 修改内容简述
// ……
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using URWPGSim2D.Common;

namespace URWPGSim2D.StrategyLoader
{
    /// <summary>The real plug-in interface we use to communicate across app-domains 
    /// for loading and unloading strategy dynamically.</summary>
    public interface IStrategy
    {
        /// <summary> 
        /// the method for getting TeamName from the strategy component (dll) to be called across app-domain
        /// </summary>
        /// <returns>
        /// returns the team name string
        /// </returns>
        string GetTeamName();

        /// <summary>
        /// 获取当前仿真使命（比赛项目）当前队伍所有仿真机器鱼的决策数据构成的数组
        /// </summary>
        /// <param name="mission">服务端当前运行着的仿真使命Mission对象</param>
        /// <param name="teamId">当前队伍在服务端运行着的仿真使命中所处的编号 
        /// 用于作为索引访问Mission对象的TeamsRef队伍列表中代表当前队伍的元素</param>
        /// <returns>当前队伍所有仿真机器鱼的决策数据构成的Decision数组对象</returns>
        Decision[] GetDecision(Mission mission, int teamId);
    }

    /// <summary>
    /// Factory class to create objects exposing IStrategyInterface
    /// </summary>
    public class StrategyInterfaceFactory : MarshalByRefObject
    {
        private const BindingFlags flag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

        public StrategyInterfaceFactory() { }

        /// <summary>Factory method to create an instance of the type whose name is specified,
        /// using the named assembly file and the constructor that best matches the specified parameters</summary>
        /// <param name="assemblyFile">The name of a file that contains an assembly where the type named typeName is sought</param>
        /// <param name="typeName">The name of the preferred type</param>
        /// <param name="constructArgs">An array of arguments that match in number, order, 
        /// and type the parameters of the constructor to invoke, or null for default constructor</param>
        /// <returns>The return value is the created object represented as IStrategyInterface</returns>
        public IStrategy Create(string assemblyFile, string typeName, object[] constructArgs)
        {
            return (IStrategy)Activator.CreateInstanceFrom(assemblyFile, typeName, false, flag, null, 
                constructArgs, null, null, null).Unwrap();
        }

        /// <summary>
        /// override the InitializeLifetimeService to return null instead of a valid ILease implementation
        /// to ensure this type of remote object never dies
        /// </summary>
        /// <returns>null</returns>
        public override object InitializeLifetimeService()
        {
            //return base.InitializeLifetimeService();
            return null; // makes the object live indefinitely
        }
    }
}
