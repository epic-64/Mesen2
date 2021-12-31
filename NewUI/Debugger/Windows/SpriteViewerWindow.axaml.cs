#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Mesen.Debugger.Controls;
using Mesen.Debugger.ViewModels;
using Mesen.Interop;
using System.ComponentModel;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Mesen.Debugger.Utilities;

namespace Mesen.Debugger.Windows
{
	public class SpriteViewerWindow : Window
	{
		private NotificationListener _listener;
		private SpriteViewerViewModel _model;
		private PixelPoint? _prevMousePos = null;

		[Obsolete("For designer only")]
		public SpriteViewerWindow() : this(CpuType.Cpu, ConsoleType.Snes) { }

		public SpriteViewerWindow(CpuType cpuType, ConsoleType consoleType)
		{
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif

			PictureViewer picViewer = this.FindControl<PictureViewer>("picViewer");
			Grid spriteGrid = this.FindControl<Grid>("spriteGrid");
			_model = new SpriteViewerViewModel(cpuType, consoleType, picViewer, spriteGrid, this);
			DataContext = _model;
		
			_model.Config.LoadWindowSettings(this);
			_listener = new NotificationListener();

			if(Design.IsDesignMode) {
				return;
			}

			picViewer.PointerMoved += PicViewer_PointerMoved;
			picViewer.PointerLeave += PicViewer_PointerLeave;
			picViewer.PositionClicked += PicViewer_PositionClicked;
			_listener.OnNotification += listener_OnNotification;
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		protected override void OnOpened(EventArgs e)
		{
			if(Design.IsDesignMode) {
				return;
			}

			_listener.OnNotification += listener_OnNotification;
			_model.RefreshData();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_listener?.Dispose();
			_model.Config.SaveWindowSettings(this);
		}

		private void PicViewer_PointerMoved(object? sender, PointerEventArgs e)
		{
			if(sender is PictureViewer viewer) {
				PixelPoint? point = viewer.GetGridPointFromMousePoint(e.GetCurrentPoint(viewer).Position);
				if(point == _prevMousePos) {
					return;
				}
				_prevMousePos = point;

				DynamicTooltip? tooltip = null;
				if(point != null) {
					DynamicTooltip? existingTooltip = ToolTip.GetTip(viewer) as DynamicTooltip;

					if(_model.CpuType == CpuType.Cpu) {
						point = point.Value.WithX(point.Value.X - 256);
					}
					SpritePreviewModel? sprite = _model.GetMatchingSprite(point.Value);
					tooltip = sprite == null ? null : _model.GetPreviewPanel(sprite, existingTooltip);
				}

				if(tooltip != null) {
					ToolTip.SetTip(viewer, tooltip);

					//Force tooltip to update its position
					ToolTip.SetHorizontalOffset(viewer, 14);
					ToolTip.SetHorizontalOffset(viewer, 15);
					ToolTip.SetIsOpen(viewer, true);
				} else {
					ToolTip.SetTip(viewer, null);
					ToolTip.SetIsOpen(viewer, false);
				}
			}
		}
		
		private void PicViewer_PointerLeave(object? sender, PointerEventArgs e)
		{
			if(sender is PictureViewer viewer) {
				ToolTip.SetTip(viewer, null);
				ToolTip.SetIsOpen(viewer, false);
			}
			_prevMousePos = null;
		}

		private void PicViewer_PositionClicked(object? sender, PositionClickedEventArgs e)
		{
			if(sender is PictureViewer viewer) {
				PixelPoint p = e.Position;
				if(_model.CpuType == CpuType.Cpu) {
					p = p.WithX(p.X - 256);
				}

				SpritePreviewModel? sprite = _model.GetMatchingSprite(p);
				_model.SelectedSprite = sprite;
				_model.UpdateSelection(sprite);
			}
			e.Handled = true;
		}

		private void OnSettingsClick(object sender, RoutedEventArgs e)
		{
			_model.Config.ShowSettingsPanel = !_model.Config.ShowSettingsPanel;
		}

		private void listener_OnNotification(NotificationEventArgs e)
		{
			ToolRefreshHelper.ProcessNotification(this, e, _model.Config.RefreshTiming, _model.CpuType, _model.RefreshData);
		}
	}
}
