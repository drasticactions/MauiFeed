﻿// <copyright file="UIUtilities.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

namespace MauiFeed.UI.Tools;

public static class UIUtilities
{
    /// <summary>
    /// Gets the System Tint.
    /// </summary>
    /// <returns><see cref="UIColor"/>.</returns>
    public static UIColor GetSystemTint()
    {
#if TVOS
        return UIColor.Clear;
#else
        return UIColor.Tint;
#endif
    }

    public static UIImage ScalePreservingAspectRatio(this UIImage sourceImage, CGSize targetSize)
    {
        // Determine the scale factor that preserves aspect ratio
        var widthRatio = targetSize.Width / sourceImage.Size.Width;
        var heightRatio = targetSize.Height / sourceImage.Size.Height;

        var scaleFactor = Math.Min(widthRatio, heightRatio);

        // Compute the new image size that preserves aspect ratio
        var scaledImageSize = new CGSize(
            width: sourceImage.Size.Width * scaleFactor,
            height: sourceImage.Size.Height * scaleFactor);

        // Render the new image
        UIGraphics.BeginImageContextWithOptions(scaledImageSize, false, 0);
        sourceImage.Draw(new CGRect(CGPoint.Empty, scaledImageSize));
        var scaledImage = UIGraphics.GetImageFromCurrentImageContext();
        UIGraphics.EndImageContext();

        return scaledImage;
    }

    public static UIImage AddBackgroundColorWithInset(this UIImage originalImage, UIColor backgroundColor, float inset)
    {
        // Calculate the new size with inset (padding)
        CGSize newSize = new CGSize(originalImage.Size.Width + (inset * 2), originalImage.Size.Height + (inset * 2));

        // Start the image context
        UIGraphics.BeginImageContextWithOptions(newSize, false, originalImage.CurrentScale);

        // Set the background color
        backgroundColor.SetFill();
        CGContext context = UIGraphics.GetCurrentContext();
        context.FillRect(new CGRect(0, 0, newSize.Width, newSize.Height));

        // Draw the original image inset within the new image
        originalImage.Draw(new CGRect(inset, inset, originalImage.Size.Width, originalImage.Size.Height));

        // Get the new image
        UIImage newImageWithBackgroundAndInset = UIGraphics.GetImageFromCurrentImageContext();

        UIGraphics.EndImageContext();

        return newImageWithBackgroundAndInset;
    }

    public static UIImage AddBackgroundWithInsetAndResize(this UIImage originalImage, UIColor backgroundColor, float inset, CGSize newSize)
    {
        // Calculate the size with inset
        CGSize sizeWithInset = new CGSize(originalImage.Size.Width + (inset * 2), originalImage.Size.Height + (inset * 2));

        // Start the image context with the size including inset
        UIGraphics.BeginImageContextWithOptions(sizeWithInset, false, originalImage.CurrentScale);

        // Set the background color
        backgroundColor.SetFill();
        CGContext context = UIGraphics.GetCurrentContext();
        context.FillRect(new CGRect(0, 0, sizeWithInset.Width, sizeWithInset.Height));

        // Draw the original image inset within the new image
        originalImage.Draw(new CGRect(inset, inset, originalImage.Size.Width, originalImage.Size.Height));

        // Get the image with background and inset
        UIImage imageWithBackgroundAndInset = UIGraphics.GetImageFromCurrentImageContext();

        // End the image context
        UIGraphics.EndImageContext();

        // Now resize this image to the new size
        UIGraphics.BeginImageContextWithOptions(newSize, false, originalImage.CurrentScale);
        imageWithBackgroundAndInset.Draw(new CGRect(0, 0, newSize.Width, newSize.Height));
        UIImage resizedImage = UIGraphics.GetImageFromCurrentImageContext();
        UIGraphics.EndImageContext();

        return resizedImage;
    }

    public static UIImage AddBackgroundWithInsetAndMargin(this UIImage originalImage, UIColor backgroundColor, float inset, float margin)
    {
        // Calculate the new size with inset and margin
        CGSize newSize = new CGSize(originalImage.Size.Width + (inset * 2) + (margin * 2), originalImage.Size.Height + (inset * 2) + (margin * 2));

        // Start the image context
        UIGraphics.BeginImageContextWithOptions(newSize, false, originalImage.CurrentScale);

        // Set the background color
        backgroundColor.SetFill();
        CGContext context = UIGraphics.GetCurrentContext();
        context.FillRect(new CGRect(0, 0, newSize.Width, newSize.Height));

        // Draw the original image inset and margin within the new image
        originalImage.Draw(new CGRect(inset + margin, inset + margin, originalImage.Size.Width, originalImage.Size.Height));

        // Get the new image
        UIImage newImageWithBackgroundInsetAndMargin = UIGraphics.GetImageFromCurrentImageContext();

        UIGraphics.EndImageContext();

        return newImageWithBackgroundInsetAndMargin;
    }

    public static UIImage AddBackgroundColorToImage(this UIImage originalImage, UIColor backgroundColor)
    {
        // Create an image context
        UIGraphics.BeginImageContextWithOptions(originalImage.Size, false, originalImage.CurrentScale);

        // Set the background color
        backgroundColor.SetFill();
        CGContext context = UIGraphics.GetCurrentContext();
        context.FillRect(new CGRect(0, 0, originalImage.Size.Width, originalImage.Size.Height));

        // Draw the original image
        originalImage.Draw(new CGPoint(0, 0));

        // Get the new image
        UIImage coloredImage = UIGraphics.GetImageFromCurrentImageContext();

        UIGraphics.EndImageContext();

        return coloredImage;
    }

    /// <summary>
    /// Convert a UIImage to one with rounded corners.
    /// </summary>
    /// <param name="image">Image.</param>
    /// <param name="radius">Radius.</param>
    /// <returns>Image with rounded corners.</returns>
    public static UIImage WithRoundedCorners(this UIImage image, nfloat? radius = null)
    {
        var maxRadius = Math.Min(image.Size.Width, image.Size.Height) / 2;
        var cornerRadius = radius.HasValue && radius.Value > 0 && radius.Value <= maxRadius
            ? radius.Value
            : maxRadius;

        UIGraphics.BeginImageContextWithOptions(image.Size, false, image.CurrentScale);
        CGRect rect = new CGRect(CGPoint.Empty, image.Size);
        UIBezierPath path = UIBezierPath.FromRoundedRect(rect, (nfloat)cornerRadius);
        path.AddClip();
        image.Draw(rect);
        UIImage roundedImage = UIGraphics.GetImageFromCurrentImageContext();
        UIGraphics.EndImageContext();

        return roundedImage;
    }
}
