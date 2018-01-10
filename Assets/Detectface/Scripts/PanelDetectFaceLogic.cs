using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Detectface.Scripts;
using UnityEngine;
using UnityEngine.UI;

public class PanelDetectFaceLogic : MonoBehaviour
{
    private const string DEFAULT_IMAGE_NAME = "picture.png";

    [SerializeField]
    public static string log = "";
    
    public GameObject panelStep1;
    public GameObject panelStep2;
    
    [SerializeField]
    public static Text txtLog;
    
    [SerializeField]
    public Text txtPoints;
    
    [SerializeField]
    public Text txtAge;
    
    [SerializeField]
    public Text txtMotion;
    

    const double EXPIRED_SECONDS = 2592000;
    // Use this for initialization
    void Start()
    {
        setParams();
        try
        {
//            txtLog = GameObject.Find("txtLog").GetComponent<Text>();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        initalizePanel();
    }

    // Update is called once per frame
    void Update()
    {
    }
    
    public WebCamTexture mCamera = null;
    public GameObject plane;

    // Use this for initialization
    public void openCamera()
    {
        //Encode to a PNG
//        byte[] bytes = photo.EncodeToPNG();
//        detectface(bytes);
    }
    
    
    [SerializeField]
    private Image _imageResult;
    

    private AspectRatioFitter _aspectRatioFitter;
    
    public void CameraPicture ()
    {
        showlog("启动拍摄");
        Native.Instance.CameraPicture (DEFAULT_IMAGE_NAME, PictureLoaded, 512);
        
        
//        file://Users/Paulo/Desktop/img_2013_008_original.jpg
        //string url = "file:///Users/leon49/Documents/unity workspace/YoutuTest/Assets/test2.jpg";
        //LoadPicture(url);
    }
    
    private void LoadPicture (string path)
    {
        StartCoroutine (LoadPictureCoroutine (path));
    }
    
    IEnumerator LoadPictureCoroutine(string path)
    {
        WWW www = new WWW(path);
        
        if (www.size == 0)
        {
            showlog("size 0");
            yield return new WaitForSeconds (1.0f);
            LoadPicture (path);
        }
        else if (!string.IsNullOrEmpty(www.error))
        {
            showlog("error");
            PictureLoaded(false, null, null);
        }
        else
        {
            ExifLib.JpegInfo jpi = ExifLib.ExifReader.ReadJpeg(www.bytes,"test2.jpg");
            showlog("EXIF: " + jpi.Orientation);
            
            Texture2D texture = new Texture2D(www.texture.width,www.texture.height, TextureFormat.RGB24, false);

            texture.anisoLevel = 1; 
            texture.filterMode = FilterMode.Bilinear; 
            texture.wrapMode = TextureWrapMode.Clamp; 

            www.LoadImageIntoTexture (texture);
            
            TextureScale.Bilinear(texture,texture.width/10,texture.height/10);
            texture = TextureRotate.RotateImage(texture,-90);

            Sprite picture = Sprite.Create (texture, new Rect (0, 0, texture.width, texture.height), Vector2.zero, 1);
            PictureLoaded(true, path, picture);
        }
    }

    
    public void testLocalImage(){
                
        string filePath = "Assets/test2.jpg";

        Texture2D texture = new Texture2D(300,200, TextureFormat.RGB24, false);

        texture.anisoLevel = 1; 
        texture.filterMode = FilterMode.Bilinear; 
        texture.wrapMode = TextureWrapMode.Clamp; 
        
        byte[] fileData = null;
 
        if (File.Exists(filePath))     {
            fileData = File.ReadAllBytes(filePath);
            texture.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }

        TextureScale.Bilinear(texture,texture.width/10,texture.height/10);
        
        Sprite picture = Sprite.Create (texture, new Rect (0, 0, texture.width, texture.height), Vector2.zero, 1);
        _imageResult.sprite = picture;

        byte[] imgData = texture.EncodeToPNG();
        detectface(imgData);
    }
    
    private void PictureLoaded(bool success, string path, Sprite sprite)
    {
        if(success)
        {
            if(_aspectRatioFitter == null)
            {
                _aspectRatioFitter = _imageResult.GetComponent <AspectRatioFitter>();
            }

            _aspectRatioFitter.aspectRatio = (float)sprite.texture.width/(float)sprite.texture.height;

            _imageResult.sprite = sprite;

            showlog("拍摄成功! ");
            
            detectface(sprite.texture.EncodeToJPG());
       

            print("SUCCESS: " + sprite.texture.width + "x" + sprite.texture.height + "px");
        }
    }


    void setParams()
    {
        string appid = "10110438";
        string secretId = "AKIDKsJbhcgtGZoslToKUUJxRkvwtnjVrcu2";
        string secretKey = "86xeJpBMPLqIcnpkW9e4OdkgRft20mNm";
        string userid = "274333243";

        Conf.Instance().setAppInfo(appid, secretId, secretKey, userid, Conf.Instance().YOUTU_END_POINT);
    }

    void detectface( byte[] inArray )
    {
        showlog("数组长度 " + inArray.Length);
        
        string expired = Utility.UnixTime(EXPIRED_SECONDS);
        string methodName = "youtu/api/detectface";
        StringBuilder postData = new StringBuilder();
        string pars = "\"app_id\":\"" + Conf.Instance().APPID +
                      "\",\"image\":\"" + Convert.ToBase64String(inArray.ToArray()) +
                      "\",\"mode\":1";
//        pars = string.Format(pars, Conf.Instance().APPID, Utility.ImgBase64(path));
        postData.Append("{");
        postData.Append(pars);
        postData.Append("}");
        string result = Http.HttpPost(methodName, postData.ToString(), Auth.appSign(expired, Conf.Instance().USER_ID));

        showlog("http 请求完成"+" 长度"+result.Length);

        print(result);
        YouTuResponseData tData = YouTuResponseData.fromJson(result);
        showlog(JsonUtility.ToJson(tData));
        if (tData !=null && tData.errorcode==0)
        {
            try
            {
                jumpToStep2();
                txtPoints.text = tData.face[0].beauty.ToString();
                txtAge.text = tData.face[0].age.ToString();
                string strMotion = "";
                int exp = tData.face[0].expression;
                if ( exp<30 )
                {
                    strMotion = "黯然伤神";
                }else if ( exp>30 && exp<70  )
                {
                    strMotion = "微微一笑";
                }
                else
                {
                    strMotion = "开怀大笑";
                }
                txtMotion.text = strMotion;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
    
    public static void showlog( string aLog )
    {
        log += aLog+"\n";
        if (txtLog != null && txtLog.IsActive())
        {
            txtLog.text = log;    
        }
    }

    public void jumpToStep2()
    {
        panelStep1.SetActive(false);
        panelStep2.SetActive(true);
    }

    public void jumpBackToStep1()
    {
        panelStep1.SetActive(true);
        panelStep2.SetActive(false);
        txtPoints.text = "";
        txtAge.text = "";
        txtMotion.text = "";
        _imageResult.sprite = null;
    }
    

    public void initalizePanel()
    {
        panelStep1.SetActive(true);
        panelStep2.SetActive(false);
    }
}