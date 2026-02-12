using Fusion;
using UnityEngine;
using TMPro;

public class PlayerCustomization : NetworkBehaviour
{
    public MeshRenderer BodyRenderer;
    public TextMeshPro NameText;

    [Networked, OnChangedRender(nameof(OnColorChanged))]
    public Color NetColor { get; set; }

    [Networked, OnChangedRender(nameof(OnNameChanged))]
    public NetworkString<_16> NetName { get; set; }

    public override void Spawned()
    {
        // IMPORTANT: apply current replicated values immediately (covers initial spawn / late join)
        OnColorChanged();
        OnNameChanged();

        // The local player (InputAuthority) knows the lobby data -> send it to StateAuthority
        if (Object.HasInputAuthority)
        {
            RPC_SetCustomization(ClientData.Instance.PlayerColor, ClientData.Instance.PlayerName);
        }
    }

    // Owner -> StateAuthority (host/authority peer) sets the Networked properties
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetCustomization(Color color, NetworkString<_16> name)
    {
        NetColor = color;
        NetName = name;

        // Optional: apply on the authority instance immediately too
        OnColorChanged();
        OnNameChanged();
    }

    private void OnColorChanged()
    {
        if (BodyRenderer != null)
            BodyRenderer.material.color = NetColor;
    }

    private void OnNameChanged()
    {
        if (NameText != null)
            NameText.text = NetName.ToString();
    }
}
