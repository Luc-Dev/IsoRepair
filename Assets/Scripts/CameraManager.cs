using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Xml.Serialization;
using UnityEngine.Animations;
using UnityEngine.UI;
using UnityEngine.UIElements;

/*
 *  TODO****************** 
 * 
 * 
 */

public class CameraManager : MonoBehaviour
{

    private Vector3 mouseWorldPos;                                                          //mouse world position

    private Vector2 delta;                                                                  //mouse location/delta 
    private Vector2 scroll;                                                                 //mouse scroll value

    private bool isRotating;                                                                //is player rotating camera
    private bool isRotationBusy;                                                            //is camera busy rotating
    private bool isZooming;                                                                 //is player zooming camera
    private bool isMouseOverGameWindow;                                                     //is mouse within game window

    private float xRotation;                                                                //x rotation of camera
    private float rotationMinOrthoSize = 6f;                                                //min othrographic size for rotating camera 
    private float zoom;                                                                     //camera zoom value
    private float zoomMultiplier = 4f;                                                      //camera zoom multiplier
    private float minZoom = 2f;                                                             //camera minimum zoom 
    private float maxZoom = 7f;                                                             //camera max zoom
    private float zoomAxisY;                                                                //mouse Y axis for player scroll input  

    [SerializeField] private float rotationSpeed = 0.5f;                                    //camera rotation speed
    [SerializeField] private Camera cam;                                                    //selected camera 


    private void Awake()
    {
        xRotation = cam.transform.rotation.eulerAngles.x;                                   //get camera x rotation
    }

    private void Start()
    {
        zoom = cam.orthographicSize;                                                        //get camera orthgraphic size
        UnityEngine.Cursor.lockState = CursorLockMode.Confined;                             //lock cursor to game screen
    }

    private void LateUpdate()
    {
        if (isRotating && cam.orthographicSize > rotationMinOrthoSize)                                                                                  //can camera rotate
        {
            cam.transform.Rotate(new Vector3(xRotation, -delta.x * rotationSpeed, 0.0f));                                                               //rotate camera
            cam.transform.rotation = Quaternion.Euler(xRotation, cam.transform.rotation.eulerAngles.y, 0.0f);                                           //camera new rotation

        }

        if (0 > Input.mousePosition.x || 0 > Input.mousePosition.y || Screen.width < Input.mousePosition.x || Screen.height < Input.mousePosition.y)    //mouse not within game screen
        {
            isMouseOverGameWindow = false;                                                  
        }
        else                                                                                                                                            //mouse within game screen
        {
            isMouseOverGameWindow = true;                                                                                                            
        }
    }

    public void OnLook(InputAction.CallbackContext context)                                 //OnLook input
    {
        delta = context.ReadValue<Vector2>();                                               //read/assign mouse delta
       
    }

    public void OnRotate(InputAction.CallbackContext context)                               //OnRotate input
    {
        if (isRotationBusy) return;                                                         //return if camera is busy rotating

        isRotating = context.started || context.performed;                                  //is rotating button pressed

        if (context.canceled)                                                               //after rotating button is pressed
        {
            isRotationBusy = true;                                                          //rotating camera is busy
            SnapRotation();                                                                 //snap camera rotation
        }
    }

    public void OnScroll(InputAction.CallbackContext context)                               //OnScroll input
    {
        scroll = context.ReadValue<Vector2>();                                              //read/assign mouse scroll value

    }

    public void OnZoom(InputAction.CallbackContext context)                                 //OnZoom input
    {

        zoomAxisY = context.ReadValue<float>();                                             //read/assign mouse Y axis

        if (zoomAxisY > 0 && isMouseOverGameWindow)                                         //mouse y axis Up && mouse within game window
        {
            isZooming = true;                                                               //is zooming

            if (GetComponent<Camera>().orthographicSize == 7)                               //if camera orthographic size is zoomed out
            {      
                SmoothZoomIn();                                                             //start SmoothZoomIn                                                 
            }
            
        }

        if (zoomAxisY < 0 && isMouseOverGameWindow)                                         //mouse Y axis Down && mouse within game window
        {
            isZooming = true;                                                               //is zooming

            if (GetComponent<Camera>().orthographicSize < 3)                                //if camera orthographic size is zoomed in
            {
                SmoothZoomOut();                                                            //start SmoothZoomOut
            } 
        }
        
    }

    private void SmoothZoomIn()                                                             //SmoothZoomIn to zoom in camera on mouse position

    {

        if (isZooming)                                                                      //camera is Zooming
        {
            zoom -= scroll.y * zoomMultiplier;                                              //mouse Y delta * zoom Multiplier
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);                                     //calculate zoom value with Mathf.Camp
        }

        Sequence zoomInSequence = DOTween.Sequence();                                                                                                   //create zoom in DOTween Sequence
        zoomInSequence.Append(cam.transform.DOMove(MousePosVector(), 0.3f).SetEase(Ease.InFlash));                                                      //start camera Move on zoom in
        zoomInSequence.Join(GetComponent<Camera>().DOOrthoSize(zoom, 0.3f).SetEase(Ease.InFlash).OnComplete(() => { isZooming = false; }));             //start camera Ortho size on zoom in

    }

    private void SmoothZoomOut()                                                            //SmoothZoomOut to zoom camera out to default position
    {
        Sequence zoomOutSequence = DOTween.Sequence();                                                                                                  //create zoom out DOTween Sequence
        zoomOutSequence.Append(cam.transform.DOMove(Vector3.zero, 0.3f).SetEase(Ease.InFlash));                                                         //start camera Move on zoom out
        zoomOutSequence.Join(GetComponent<Camera>().DOOrthoSize(maxZoom, 0.3f).SetEase(Ease.InFlash).OnComplete(() => { isZooming = false; }));         //start camera Othro size on zoom out

    }

    private Vector3 MousePosVector()                                                        //MousePosVector to get mouse position on game screen
    {
        mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);                        //get mouse position on game screen
            
        return mouseWorldPos;                                                               //return mouse position
    }

    private void SnapRotation()                                                             //SnapRotation to rotate camera to specific position
    {
        cam.transform.DORotate(SnappedVector(), 0.5f).SetEase(Ease.OutBack).OnComplete(() => { isRotationBusy = false; });                              //start DOTween camera roation 
    }

    private Vector3 SnappedVector()                                                         //SnappedVector to determine where camera rotaion snaps
    {
        var endValue = 0.0f;                                                                //end value
        var currentY = MathF.Ceiling(cam.transform.rotation.eulerAngles.y);                 //Mathf.ceiling for camera rotation

        endValue = currentY switch                                                          //Switch Statement to set endValue to camera rotation Y position 
        {
            >= 0 and <= 90 => 45.0f,
            >= 91 and <= 180 => 135.0f,
            >= 181 and <= 270 => 225.0f,
            _ => 315.0f,
        };

        return new Vector3(xRotation, endValue, 0.0f);                                      //return Vector3 with camera x rotation, endValue, set y to zero
    }
}
