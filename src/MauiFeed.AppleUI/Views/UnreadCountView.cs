﻿// <copyright file="UnreadCountView.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using ObjCRuntime;

namespace MauiFeed.UI.Views;

public class UnreadCountView : UICellAccessoryCustomView
{
    UILabel unreadLabel;

    public UnreadCountView(UIView customView)
        : base(customView, UICellAccessoryPlacement.Trailing)
    {
        this.unreadLabel = (UILabel)customView;
    }

    public void SetUnreadCount(int count)
    {
        this.unreadLabel.Text = count.ToString();
        this.IsHidden = count == 0;
    }
}