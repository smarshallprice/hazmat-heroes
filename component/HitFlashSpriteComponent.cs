using Godot;
using MatchTastic;
using System;

public partial class HitFlashSpriteComponent : Sprite2D
{

	[Export]
	public HealthComponent HealthComponent { get; set; } = null;

	Tween shaderTween;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (IsMultiplayerAuthority())
		{			
			HealthComponent.Damaged += OnDamaged;
		}
	}


	//right now this only gets called on server, which could cause a delay on the shoot client if not a the server,
	//we should look into playing this on the local client for better feedback, buit also calling the server
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	void PlayHighlight()
	{
		if (shaderTween != null && shaderTween.IsValid())
		{
			shaderTween.Kill();
		}

		shaderTween = CreateTween();
		shaderTween.TweenProperty(Material, "shader_parameter/percent", 0, .2)
		.From(1)
		.SetTrans(Tween.TransitionType.Quint)
		.SetEase(Tween.EaseType.In);
	}

	void OnDamaged()
	{
		PlayHighlightRpc();
		//Rpc(MethodName.PlayHighlight);
	}
}
