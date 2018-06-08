using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

public class InputManager : Manager {
    // Rewired info
	private const int PLAYER_ID = 0;
	private Player rewiredPlayer;

    // Input value references
    public Vector2 MoveInput { get { return moveInput; } }
    private Vector2 moveInput;
    public Vector2 LookInput { get { return lookInput; } }
    private Vector2 lookInput;
    public float ZoomInput { get { return zoomInput; } }
    private float zoomInput;

    private Vector2 keyboardMoveInput = Vector2.zero;

	public override void Startup()
    {
        rewiredPlayer = ReInput.players.GetPlayer(PLAYER_ID);

        moveInput = Vector2.zero;
        lookInput = Vector2.zero;

        managerState = ManagerState.Started;
    }

    public override void Shutdown()
    {
        managerState = ManagerState.Shutdown;
    }

    private void Update() {
        if (rewiredPlayer == null) {
            return;
        }
        
        HandleMoveInput();
        HandleLookInput();
    }

    private void HandleMoveInput() {
        keyboardMoveInput.x = rewiredPlayer.GetAxisRaw("KeyboardMoveLateral");
        keyboardMoveInput.y = rewiredPlayer.GetAxisRaw("KeyboardMoveVertical");

        if (keyboardMoveInput.sqrMagnitude > 0f) {
            moveInput = keyboardMoveInput;
        }
        else {
            moveInput.x = rewiredPlayer.GetAxis("MoveLateral");
            moveInput.y = rewiredPlayer.GetAxis("MoveVertical");
        }
    }

    private void HandleLookInput() {
        if (rewiredPlayer.GetButton("MouseLookToggle")) {
            lookInput.x = rewiredPlayer.GetAxis("MouseLookLateral");
            lookInput.y = rewiredPlayer.GetAxis("MouseLookVertical");
            lookInput = lookInput.normalized;
        }
        else if (rewiredPlayer.GetButton("ZoomToggle")) {
            // Consume all input while the user is zooming with the toggle.
            zoomInput = rewiredPlayer.GetAxis("LookVertical");
            lookInput = Vector2.zero;
        }
        else {
            lookInput.x = rewiredPlayer.GetAxis("LookLateral");
            lookInput.y = rewiredPlayer.GetAxis("LookVertical");
            zoomInput = rewiredPlayer.GetAxis("MouseScroll");
        }
    }
}
