using System;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.OS;
using Com.Umeng.Message;
using Android.Util;
using Android.Graphics;
using Com.Umeng.Common.Message;
using System.Threading.Tasks;
using Com.Umeng.Message.Local;
using Android.Views.InputMethods;
using System.Collections.Generic;
using System.Text;
using Com.Umeng.Message.Tag;

namespace UMeng.Push.Demo
{
	[Activity(Label = "UMeng.Push.Demo", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity, IUmengRegisterCallback, IUmengUnregisterCallback
	{
		protected const string Tag = "MainActivity";

		private EditText edTag, edAlias, edExclusiveAlias, edAliasType;
		private TextView tvStatus, infoTextView;
		private ImageView btnEnable;
		private Button btnAddTag, btnListTag, btnAddAlias, btnAddExclusiveAlias, btnLocalNotification;
		private ProgressDialog dialog;
		private Spinner spAliasType;

		private PushAgent _pushAgent;

		private bool edAliasTypeFocus;

		public Handler handler = new Handler();

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			Window.SetSoftInputMode(SoftInput.AdjustPan);
			DisplayMetrics dm = new DisplayMetrics();
			WindowManager.DefaultDisplay.GetMetrics(dm);

			SetContentView(Resource.Layout.activity_main);

			PrintKeyValue();

			_pushAgent = PushAgent.GetInstance(this);

			//_pushAgent.PushCheck = true; 默认不检查继承配置文件
			//_pushAgent.LocalNotificationIntervalLimit = false; 默认本地通知间隔最少是10分钟

			_pushAgent.NotificationPlaySound = MsgConstant.NotificationPlaySdkEnable; //开启通知声音

			//_pushAgent.NotificationPlaySound = MsgConstant.NotificationPlaySdkDisable  //关闭通知声音
			//_pushAgent.NotificationPlaySound = MsgConstant.NotificationPlayServer //通知声音由服务端控制

			//_pushAgent.NotificationPlayLights = MsgConstant.NotificationPlaySdkDisable; //通知禁用闪光
			//_pushAgent.NotificationPlayVibrate = MsgConstant.NotificationPlaySdkDisable //通知禁用震动

			//应用程序启动统计
			_pushAgent.OnAppStart();

			//开启推送并设置注册的回调处理
			_pushAgent.Enable(this);

			tvStatus = FindViewById<TextView>(Resource.Id.tvStatus);
			btnEnable = FindViewById<ImageView>(Resource.Id.btnEnable);
			btnAddTag = FindViewById<Button>(Resource.Id.btnAddTags);
			btnAddAlias = FindViewById<Button>(Resource.Id.btnAddAlias);
			btnAddExclusiveAlias = FindViewById<Button>(Resource.Id.btnAddExclusiveAlias);
			btnListTag = FindViewById<Button>(Resource.Id.btnListTags);
			btnLocalNotification = FindViewById<Button>(Resource.Id.btnLocalNotification);
			infoTextView = FindViewById<TextView>(Resource.Id.info);
			edTag = FindViewById<EditText>(Resource.Id.edTag);
			edAlias = FindViewById<EditText>(Resource.Id.edAlias);
			edExclusiveAlias = FindViewById<EditText>(Resource.Id.edExclusiveAlias);
			edAliasType = FindViewById<EditText>(Resource.Id.edAliasType);
			spAliasType = FindViewById<Spinner>(Resource.Id.spAliasType);

			edAliasType.FocusChange += (s, e) =>
			{
				edAliasTypeFocus = e.HasFocus;
			};

			edAliasType.TextChanged += (s, e) =>
			{
				if (edAliasTypeFocus)
				{
					spAliasType.SetSelection(0);
				}
			};

			string[] aliasType = new string[] { "Alias Type:",ALIAS_TYPE.SinaWeibo,ALIAS_TYPE.Baidu,
			ALIAS_TYPE.Kaixin,ALIAS_TYPE.Qq,ALIAS_TYPE.Renren,ALIAS_TYPE.TencentWeibo,ALIAS_TYPE.Weixin};
			ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItemSingleChoice, aliasType);
			spAliasType.Adapter = adapter;
			spAliasType.SetBackgroundColor(Color.LightGray);
			spAliasType.ItemSelected += (s, e) =>
			{
				TextView tv = (TextView)e.View;
				if (tv != null)
				{
					float textSize = 15f;
					tv.TextSize = textSize;
				}

				if (e.Position != 0)
				{
					string type = spAliasType.GetItemAtPosition(e.Position).ToString();
					edAliasType.Text = type;
				}
				else if (!edAliasTypeFocus)
				{
					edAliasType.Text = "";
				}
			};

			tvStatus.Click += ClickListener;
			btnEnable.Click += ClickListener;
			btnAddTag.Click += ClickListener;
			btnListTag.Click += ClickListener;
			btnAddAlias.Click += ClickListener;
			btnAddExclusiveAlias.Click += ClickListener;
			btnLocalNotification.Click += ClickListener;

			UpdateStatus();

			//此处是完全自定义处理设置
			//_pushAgent.SetPushIntentServiceClass();
		}

		private void PrintKeyValue()
		{
			Bundle bundle = Intent.Extras;
			if (bundle != null)
			{
				var keySet = bundle.KeySet();
				foreach (string key in keySet)
				{
					string value = bundle.GetString(key);
					Android.Util.Log.Info(Tag, $"{key}:{value}");
				}
			}
		}

		private void SwitchPush()
		{
			if (btnEnable.Clickable)
			{
				btnEnable.Clickable = false;
				string info = $"enabled:{_pushAgent.IsEnabled} isRegistered:{_pushAgent.IsRegistered}";
				Android.Util.Log.Info(Tag, $"switch Push:{info}");

				if (_pushAgent.IsEnabled || UmengRegistrar.IsRegistered(this))
				{
					_pushAgent.Disable(this);
				}
				else
				{
					_pushAgent.Enable(this);
				}
			}
		}

		private void UpdateStatus()
		{
			string pkgName = ApplicationContext.PackageName;
			string info = $"enabled:{_pushAgent.IsEnabled}\nisRegistered:{_pushAgent.IsRegistered}\nDeviceToken:{_pushAgent.RegistrationId}\nSdkVersion:{MsgConstant.SdkVersion}\nAppVersionCode:{UmengMessageDeviceConfig.GetAppVersionCode(this)}\nAppVersionName:{UmengMessageDeviceConfig.GetAppVersionName(this)}";
			tvStatus.Text = $"应用包名：{pkgName}\n{info}";

			btnEnable.SetImageResource(_pushAgent.IsEnabled ? Resource.Drawable.open_button : Resource.Drawable.close_button);
			CopyToClipBoard();

			Android.Util.Log.Info(Tag, $"updateStatus:enabled:{_pushAgent.IsEnabled},isRegistered:{_pushAgent.IsRegistered}");
			Android.Util.Log.Info(Tag, $"=============================");
			btnEnable.Clickable = true;
		}

		private void CopyToClipBoard()
		{
			if (Build.VERSION.SdkInt < BuildVersionCodes.Honeycomb)
				return;
			string deviceToken = _pushAgent.RegistrationId;
			if (!string.IsNullOrEmpty(deviceToken))
			{
				ClipboardManager clipboard = (ClipboardManager)GetSystemService(ClipboardService);
				clipboard.Text = deviceToken;
				Toast.MakeText(this, "DeviceToken已经复制到剪贴板了", ToastLength.Short).Show();
			}
		}

		private async Task AddTag()
		{
			string tag = edTag.Text;
			if (string.IsNullOrEmpty(tag))
			{
				Toast.MakeText(this, "请先输入Tag", ToastLength.Short).Show();
				return;
			}
			if (!_pushAgent.IsRegistered)
			{
				Toast.MakeText(this, "抱歉，还未注册", ToastLength.Short).Show();
				return;
			}

			ShowLoading();
			await AddTagTask(tag);
			HideInputKeyboard();
		}

		private async Task ListTags()
		{
			if (!_pushAgent.IsRegistered)
			{
				Toast.MakeText(this, "抱歉，还为注册", ToastLength.Short).Show();
				return;
			}
			ShowLoading();
			await ListTagTask();
		}

		private void LocalNotification()
		{
			Intent intent = new Intent(this, typeof(LocalNotificationActivity));
			StartActivity(intent);
		}

		private async Task AddAlias()
		{
			string alias = edAlias.Text;
			string aliasType = edAliasType.Text;
			if (string.IsNullOrEmpty(alias))
			{
				Toast.MakeText(this, "请先输入Alias", ToastLength.Short).Show();
				return;
			}

			if (string.IsNullOrEmpty(aliasType))
			{
				Toast.MakeText(this, "请先输入Alias Type", ToastLength.Short).Show();
				return;
			}

			if (!_pushAgent.IsRegistered)
			{
				Toast.MakeText(this, "抱歉，还未注册", ToastLength.Short).Show();
				return;
			}
			ShowLoading();
			await AddAliasTask(alias, aliasType);
			HideInputKeyboard();
		}

		private void ShowLoading()
		{
			if(dialog == null)
			{
				dialog = new ProgressDialog(this);
				dialog.SetMessage("Loading");
			}
			dialog.Show();
		}

		private async void ClickListener(object sender, EventArgs e)
		{
			View v = (View)sender;

			if (v == btnAddAlias)
			{
				await AddTag();
			}
			else if (v == btnAddAlias)
			{
				await AddAlias();
			}
			else if (v == btnAddExclusiveAlias)
			{
				await AddExclusiveAlias();
			}
			else if (v == btnListTag)
			{
				await ListTags();
			}
			else if (v == btnEnable)
			{
				UpdateStatus();
			}
			else if (v == btnLocalNotification)
			{
				LocalNotification();
			}
		}

		private async Task AddExclusiveAlias()
		{
			string exclusiveAlias = edExclusiveAlias.Text;
			string aliasType = edAliasType.Text;
			if (string.IsNullOrEmpty(exclusiveAlias))
			{
				Toast.MakeText(this, "请先输入Exclusive Alias", ToastLength.Short).Show();
				return;
			}
			if (string.IsNullOrEmpty(aliasType))
			{
				Toast.MakeText(this, "请先输入Alias Type", ToastLength.Short).Show();
				return;
			}
			if (!_pushAgent.IsRegistered)
			{
				Toast.MakeText(this, "抱歉，还未注册", ToastLength.Short).Show();
				return;
			}
			ShowLoading();
			await AddExclusiveAliasTask(exclusiveAlias, aliasType);
			HideInputKeyboard();
		}

		#region IUmengRegisterCallback Impl

		public void OnRegistered(string p0)
		{
			handler.Post(() =>
			{
				UpdateStatus();
			});
		}

		#endregion

		#region IUmengUnregisterCallback Immpl

		public void OnUnregistered(string p0)
		{
			handler.PostDelayed(() =>
			{
				UpdateStatus();
			}, 2000);
		}

		#endregion

		private async Task AddTagTask(string tag)
		{
			var tags = tag.Split(',');
			var result = await Task<string>.Factory.StartNew(() =>
			{
				try
				{
					TagManager.Result r = _pushAgent.TagManager.Add(tags);
					Android.Util.Log.Debug(Tag, r.ToString());
					return r.ToString();
				}
				catch (Exception e)
				{
					throw e;
				}
			});

			edTag.Text = result;
			UpdateInfo($"Add Tag:\n{result}");
		}

		private async Task AddAliasTask(string aliasString, string aliasTypeString)
		{
			var result = await Task<bool>.Factory.StartNew(() =>
			{
				try
				{
					return _pushAgent.AddAlias(aliasString, aliasTypeString);
				}
				catch (Exception e)
				{
					throw e;
				}
			});

			if (result)
			{
				Android.Util.Log.Info(Tag, "alias was set successfully.");
			}
			edAlias.Text = "";
			UpdateInfo($"Add Alias:{(result ? "Success" : "Fail")}");
		}

		private async Task AddExclusiveAliasTask(string aliasString, string aliasTypeString)
		{
			var result = await Task<bool>.Factory.StartNew(() =>
			{
				try
				{
					return _pushAgent.AddExclusiveAlias(aliasString, aliasTypeString);
				}
				catch(Exception e)
				{
					throw e;
				}
			});

			if(result)
			{
				Android.Util.Log.Info(Tag, "exclusive alias was set successfully.");
			}
			edExclusiveAlias.Text = "";
			UpdateInfo($"Add Exclusive Alias:{(result ? "Success" : "Fail")}");
		}

		private async Task ListTagTask()
		{
			var result = await Task<IList<string>>.Factory.StartNew(() =>
			{
				IList<string> tags = new List<string>();
				try
				{
					tags = _pushAgent.TagManager.List();
					Android.Util.Log.Debug(Tag, $"list tags:{string.Join(",", Tag)}");
				}
				catch (Exception e)
				{
					throw e;
				}
				return tags;
			});

			if(result != null)
			{
				var info = new StringBuilder();
				info.Append("Tags:\n");
				for (int i = 0; i < result.Count; i++)
				{
					string tag = result[i];
					info.Append(tag + "\n");
				}
				info.Append("\n");
				UpdateInfo(info.ToString());
			}
			else
			{
				UpdateInfo("");
			}
		}

		private void UpdateInfo(string v)
		{
			if (dialog != null && dialog.IsShowing)
			{
				dialog.Dismiss();
			}
			infoTextView.Text = v;
		}

		private void HideInputKeyboard()
		{
			((InputMethodManager)GetSystemService(InputMethodService)).HideSoftInputFromWindow(CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways);
		}

		private void AddLocalNotification()
		{
			UmengLocalNotification localNotification = new UmengLocalNotification();
			//设置通知开始时间
			localNotification.DateTime = DateTime.Now.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss");
			//开始时间为特殊节日
			//localNotification.Year = 2016;
			//localNotification.Hour = 12;
			//localNotification.Minute = 12;
			//localNotification.Second = 12;
			//localNotification.SpecialDay = UmengLocalNotification.ChineseNewYear;

			//设置重复次数，默认是1
			localNotification.RepeatingNum = 100;
			//设置间隔时间，默认是1
			localNotification.RepeatingInterval = 2;
			//设置重复单位，默认是天
			localNotification.RepeatingUnit = UmengLocalNotification.RepeatingUnitHour;

			//初始化通知样式
			UmengNotificationBuilder builder = localNotification.NotificationBuilder;
			//设置小图标
			builder.SmallIconDrawable = "Icon";
			//设置大图标
			builder.LargeIconDrawable = "Icon";
			//设置自动清除
			builder.Flags = (int)NotificationFlags.AutoCancel;

			localNotification.NotificationBuilder = builder;

			_pushAgent.AddLocalNotification(localNotification);
		}
	}
}

