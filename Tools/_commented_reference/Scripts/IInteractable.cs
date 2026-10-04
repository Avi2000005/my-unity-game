using UnityEngine;

namespace Echoes.Painterly
{

    public interface IInteractable
    {
    	string Prompt { get; }

    	float Range { get; }

    	Transform At { get; }

    	bool CanInteract { get; }

    	void Interact(AriMover by);
    }
}