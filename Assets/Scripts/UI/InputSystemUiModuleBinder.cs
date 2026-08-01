using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Binds UI modules to the project's canonical InputActionAsset at runtime.
/// This avoids storing importer-specific action sub-asset file IDs in scenes.
/// </summary>
[RequireComponent(typeof(InputSystemUIInputModule))]
public sealed class InputSystemUiModuleBinder : MonoBehaviour
{
    [SerializeField] private InputActionAsset actions;
    private readonly List<InputActionReference> _runtimeReferences = new();

    private void Awake()
    {
        var module = GetComponent<InputSystemUIInputModule>();
        if (actions == null)
        {
            Debug.LogError($"{nameof(InputSystemUiModuleBinder)} needs {nameof(actions)} on {name}.", this);
            return;
        }

        module.actionsAsset = actions;
        module.point = ActionReference("UI/Point");
        module.move = ActionReference("UI/Navigate");
        module.submit = ActionReference("UI/Submit");
        module.cancel = ActionReference("UI/Cancel");
        module.leftClick = ActionReference("UI/Click");
        module.middleClick = ActionReference("UI/MiddleClick");
        module.rightClick = ActionReference("UI/RightClick");
        module.scrollWheel = ActionReference("UI/ScrollWheel");
        module.trackedDevicePosition = ActionReference("UI/TrackedDevicePosition");
        module.trackedDeviceOrientation = ActionReference("UI/TrackedDeviceOrientation");
    }

    private InputActionReference ActionReference(string path)
    {
        var action = actions.FindAction(path, throwIfNotFound: false);
        if (action == null)
        {
            Debug.LogError($"Input action '{path}' is missing from {actions.name}.", this);
            return null;
        }

        var reference = InputActionReference.Create(action);
        _runtimeReferences.Add(reference);
        return reference;
    }

    private void OnDestroy()
    {
        foreach (var reference in _runtimeReferences)
        {
            if (reference != null)
                Destroy(reference);
        }

        _runtimeReferences.Clear();
    }
}
