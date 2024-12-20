﻿using ArchiToolkit.Test.Const.Type;

namespace ArchiToolkit.Test.Const;

public class MethodInvokeTest
{
    public TestClass Property { get; set; } = new();

    private void MethodInMethodTest([Const(Type = ConstType.Self)]int i)
    {
        i = 10;
        [Const(Type = ConstType.Self)]
        void Function([Const(Type = ConstType.Self)]int i)
        {
            i = 10;
            Property.NothingMethod();
            this.NothingMethod();
            NothingMethod();
            SelfMethod();
            MembersMethod();
            MembersInMembersMethod();
        }

        [Const(Type = ConstType.Self)]
        void NothingMethod()
        {
 
        }
        
    }
    

    [Const(Type = ConstType.Self)]
    private void MethodInvokeTestSelf()
    {
        Property.NothingMethod();
        NothingMethod();
        SelfMethod();
        MembersMethod();
        MembersInMembersMethod();
    }

    
    #region Using Members
    private void NothingMethod()
    {

    }

    [Const(Type = ConstType.Self)]
    private void SelfMethod()
    {

    }

    [Const(Type = ConstType.Members)]
    private void MembersMethod()
    {

    }

    [Const(Type = ConstType.MembersInMembers)]
    private void MembersInMembersMethod()
    {

    }
    #endregion
}