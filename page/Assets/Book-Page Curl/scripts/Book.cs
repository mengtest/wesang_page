using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
public enum FlipMode
{
    RightToLeft,
    LeftToRight
}
[ExecuteInEditMode]
public class Book : MonoBehaviour {
    public Canvas canvas;
    [SerializeField]
    RectTransform BookPanel;
	public Sprite background;
    public Sprite[] bookPages;
    public bool interactable=true;
    public bool enableShadowEffect=true;

    //represent the index of the sprite shown in the right page
    public int currentPage = 0;

    public int TotalPageCount
    {
        get { return bookPages.Length; }
    }
    public Vector3 EndBottomLeft
    {
        get { return ebl; }
    }
    public Vector3 EndBottomRight
    {
        get { return ebr; }
    }
    public float Height
    {
        get { return BookPanel.rect.height; }
    }

	//mask
    public Image ClippingPlane;
    public Image NextPageClip;

    public Image Shadow;
    public Image ShadowLTR;
    public Image Left;
    public Image Right;
    public Image PageBack;

    public UnityEvent OnFlip;

    float radius1, radius2;

    //书脊底部
    Vector3 sb;
    //书脊顶部
    Vector3 st;
    //corner of the page
    Vector3 c;
    //Edge Bottom Right
    Vector3 ebr;
    //Edge Bottom Left
    Vector3 ebl;
    //右下角
    Vector3 ebt;
    //follow point 
    Vector3 f;

    bool pageDragging = false;
    
    FlipMode mode;

    void Start()
    {
        float scaleFactor = 1;
        //if (canvas) scaleFactor = canvas.scaleFactor;
        float pageWidth = BookPanel.rect.width * scaleFactor ;
        float pageHeight =(BookPanel.rect.height* scaleFactor - 1) / 2;
        //float pageHeight = BookPanel.rect.height * scaleFactor ;
        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);

		Vector3 globalsb = BookPanel.transform.position + new Vector3(- pageWidth / 2 , 0);
		sb = transformPoint(globalsb);
		Vector3 globalebr = BookPanel.transform.position + new Vector3(- pageWidth / 2, - pageHeight);
		ebr = transformPoint(globalebr);
		Vector3 globalebl = BookPanel.transform.position + new Vector3(- pageWidth / 2, pageHeight);
		ebl = transformPoint(globalebl);
		Vector3 globalst = BookPanel.transform.position + new Vector3(pageWidth / 2, 0);
		st = transformPoint(globalst);
        Vector3 globalebt = BookPanel.transform.position + new Vector3(pageWidth / 2, - pageHeight);
        ebt = transformPoint(globalebt);
        //globalst是世界坐标，在transformPoint函数中，变换成自身坐标

        radius1 = Vector2.Distance(sb, ebr);

        float scaledPageWidth = pageWidth / scaleFactor;
        float scaledPageHeight = pageHeight / scaleFactor;

        radius2 = Mathf.Sqrt(scaledPageWidth * scaledPageWidth + scaledPageHeight * scaledPageHeight);

		ClippingPlane.rectTransform.sizeDelta = new Vector2(scaledPageWidth * 2, scaledPageHeight + scaledPageWidth * 2);
		Shadow.rectTransform.sizeDelta = new Vector2(scaledPageWidth, scaledPageHeight + scaledPageWidth * 0.6f);
		ShadowLTR.rectTransform.sizeDelta = new Vector2(scaledPageWidth, scaledPageHeight + scaledPageWidth * 0.6f);
		NextPageClip.rectTransform.sizeDelta = new Vector2(scaledPageWidth  , scaledPageHeight + scaledPageWidth * 0.6f);

    }

	//InverseTransformPoint变换位置从世界坐标到自身坐标。和Transform.TransformPoint相反。
    public Vector3 transformPoint(Vector3 global)
    {
        Vector2 localPos = BookPanel.InverseTransformPoint(global);
		//就是global的世界坐标变化成，相对于BookPanel的自身坐标
        //RectTransformUtility.ScreenPointToLocalPointInRectangle(BookPanel, global, null, out localPos);
        return localPos;
    }  

    void Update()
    {
        if (pageDragging&&interactable)
        {
            UpdateBook();
        }
    }

    //判断是否超出，自动翻开
    void MouseBeyond()
    {        
        int flagS = IfMouseBeyond();
        //||flagS > 0 && mode == FlipMode.LeftToRight
        //if (flagS < 0 && mode == FlipMode.RightToLeft)
        if (flagS == 1)
            ReleasePage(flagS); 
    }

    public void UpdateBook()
    {
        f= Vector3.Lerp(f , transformPoint( Input.mousePosition), Time.deltaTime * 10);
        //鼠标与之前位置的一个比例位置，就是鼠标的位置，此时鼠标的坐标是相对坐标

        if (mode == FlipMode.RightToLeft) {
			UpdateBookRTLToPoint (f);
		} else {
			UpdateBookLTRToPoint (f);
		}
        //判断是否超出，自动翻开
        //MouseBeyond();
    }
   
    //右下角的点
    public void UpdateBookLTRToPoint(Vector3 followLocation)
    {
        mode = FlipMode.LeftToRight;
        f = followLocation;

        ShadowLTR.transform.SetParent(NextPageClip.transform, true);
        ShadowLTR.transform.localPosition = new Vector3(0, 0, 0);
        ShadowLTR.transform.localEulerAngles = new Vector3(0, 0, 0);


        Left.transform.SetParent(ClippingPlane.transform, true);
        Right.transform.SetParent(BookPanel.transform, true);
        //LeftNext.transform.SetParent(BookPanel.transform, true);
        c = Calc_C_Position(followLocation);

        Vector3 t1;
        float T0_T1_Angle = Calc_T0_T1_Angle(c,ebt,out t1);
        //if (T0_T1_Angle < 0) T0_T1_Angle += 180;

        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);
        ClippingPlane.transform.eulerAngles = new Vector3(0, 0, T0_T1_Angle - 90);//遮罩
        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

        NextPageClip.transform.eulerAngles = new Vector3(0, 0, T0_T1_Angle + 90);//隐藏
        NextPageClip.transform.position = BookPanel.TransformPoint(t1);

        //page position and angle
        float C_T1_dy = t1.y - c.y;
        float C_T1_dx = t1.x - c.x;
        float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;

		Left.transform.position =BookPanel.TransformPoint(c);
        Left.transform.eulerAngles = new Vector3(0, 0, C_T1_Angle - 90);//显示的牌



        //LeftNext.transform.SetParent(NextPageClip.transform, true);
        Right.transform.SetParent(ClippingPlane.transform, true);
        Right.transform.SetAsFirstSibling();
        ShadowLTR.rectTransform.SetParent(Left.rectTransform, true);
    }
		
    public void UpdateBookRTLToPoint(Vector3 followLocation)
    {
        mode = FlipMode.RightToLeft;
        f = followLocation;

        Shadow.transform.SetParent(ClippingPlane.transform, true);
        Shadow.transform.localPosition = new Vector3(0, 0, 0);
        Shadow.transform.localEulerAngles = new Vector3(0, 0, 0);

        Right.transform.SetParent(ClippingPlane.transform, true);        
        Left.transform.SetParent(BookPanel.transform, true);
        PageBack.transform.SetParent(BookPanel.transform, true);
		c = Calc_C_Position(followLocation);

        Vector3 t1;
        float T0_T1_Angle = Calc_T0_T1_Angle(c,ebr,out t1);
//        if (T0_T1_Angle >= -90) T0_T1_Angle -= 180;

        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);
        ClippingPlane.transform.eulerAngles = new Vector3(0, 0, T0_T1_Angle + 90);
        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

        float C_T1_dy = t1.y - c.y;
        float C_T1_dx = t1.x - c.x;
		float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;

		//将page位置变换
		//TransformPoint：变换位置从自身坐标到世界坐标
		Right.transform.position = BookPanel.TransformPoint(c);
		Right.transform.eulerAngles = new Vector3(0, 0, C_T1_Angle + 90);

        NextPageClip.transform.eulerAngles = new Vector3(0, 0, T0_T1_Angle + 90);
        NextPageClip.transform.position = BookPanel.TransformPoint(t1);
        PageBack.transform.SetParent(NextPageClip.transform, true);
        Left.transform.SetParent(ClippingPlane.transform, true);
        Left.transform.SetAsFirstSibling();
        Shadow.rectTransform.SetParent(Right.rectTransform, true);
    }

	//c:鼠标坐标（已被限制范围）；bookCorner:ebr/ebt；t1:涉及ClippingPlane的坐标
	//返回的角度涉及到ClippingPlane
	//作用：随着鼠标的移动，遮罩跟着移动，主要是角度
    private float Calc_T0_T1_Angle(Vector3 c,Vector3 bookCorner,out Vector3 t1)
    {
		Vector3 Calc_SB = sb;

		//作用于，区别计算左右起始点的不同
		if (mode == FlipMode.LeftToRight) {
			bookCorner = ebt;
			Calc_SB = st;
		}
        //以下开始计算t1
		Vector3 t0 = (c + bookCorner) / 2;
      	float T0_CORNER_dy = bookCorner.y - t0.y;
      	float T0_CORNER_dx = bookCorner.x - t0.x;
        float T0_CORNER_Angle = Mathf.Atan2(T0_CORNER_dx, T0_CORNER_dy);

		//!!??!!??!!??!!??!!??!!??!!??!!??!!??!!??!!??!!??!!??!!??!!??!!??
        float T1_Y = t0.y - T0_CORNER_dx * Mathf.Tan(T0_CORNER_Angle);

		T1_Y = normalizeT1Y(T1_Y, bookCorner, Calc_SB);
		t1 = new Vector3(Calc_SB.x, T1_Y, 0);
		//以上，计算 t1（涉及到ClippingPlane的位置）

		//以下，计算ClippingPlane的角度(与t1相关)
		float T0_T1_dy = t1.x - t0.x;
		float T0_T1_dx = t1.y - t0.y;
        float T0_T1_Angle = Mathf.Atan2(T0_T1_dx, T0_T1_dy) * Mathf.Rad2Deg;

		if(c.x < Calc_SB.x && mode == FlipMode.RightToLeft||c.x > Calc_SB.x && mode == FlipMode.LeftToRight){ 
			if (T0_T1_Angle >= -90)  T0_T1_Angle -= 180;
		}
		return  T0_T1_Angle;

    }

	//修正计算(正常化？)
	//t1: 计算得出的 t1的 Y ；corner:ebr
    private float normalizeT1Y(float t1dy,Vector3 corner,Vector3 sb)
    {
        //左边点
		if (t1dy > sb.y && sb.y > corner.y) 
			return sb.y;		
		//sb.y恒大于corner.y 修正：左边两边的计算不一样
        //右边点
		if (t1dy < sb.y && sb.y < corner.y)
            return sb.y;
		
		return t1dy;
    }

	//限制鼠标的范围
	private Vector3 Calc_C_Position(Vector3 followLocation)
    {
        Vector3 c;
        f = followLocation;        
		Vector3 Calc_SB = sb;
		Vector3 Calc_ST = st; 

		if (mode == FlipMode.LeftToRight) {
			 Calc_SB = st;
			 Calc_ST = sb;
		}
		//限制范围在sb(st)为圆心，radius1为半径的圆内
		float F_SB_dy = f.y - Calc_SB.y;
		float F_SB_dx = f.x - Calc_SB.x;
        float F_SB_Angle = Mathf.Atan2(F_SB_dy, F_SB_dx);
		// r1在sb-f这条线上
		Vector3 r1 = new Vector3(radius1 * Mathf.Cos(F_SB_Angle) + Calc_SB.x , radius1 * Mathf.Sin(F_SB_Angle), 0);
		float F_SB_distance = Vector2.Distance(f, Calc_SB);
		if (F_SB_distance < radius1)			c = f;
		else			c = r1;

		//ebl-sb-ebr左边的范围限制
		float C_ST_dy = c.y - Calc_ST.y;
		float C_ST_dx = c.x - Calc_ST.x;
		float C_ST_Angle = Mathf.Atan2(C_ST_dy, C_ST_dx);
		Vector3 r2 = new Vector3(radius2 * Mathf.Cos(C_ST_Angle) + Calc_ST.x , radius2 * Mathf.Sin(C_ST_Angle), 0);
		float C_ST_distance = Vector2.Distance(c, Calc_ST);
		if(C_ST_distance>radius2){
			c = r2;
		}

        return c;
    }


	//以下为，鼠标点击时候
    public void DragRightPageToPoint(Vector3 point)
    {
        pageDragging = true;
        mode = FlipMode.RightToLeft;
        f = point;
		//point,f 就是鼠标的坐标

        NextPageClip.rectTransform.pivot = new Vector2(0, 0.12f);
        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);

        Left.gameObject.SetActive(true);
//      Left.rectTransform.pivot = new Vector2(0, 0);
        Left.transform.position = PageBack.transform.position;
        Left.transform.eulerAngles = new Vector3(0, 0, 0);
        Left.sprite =  bookPages[0];
		//sprite精灵图片
        Left.transform.SetAsFirstSibling();
		//将该对象移到同级对象的首位        

        Right.gameObject.SetActive(true);
		//设定Right的位置和RightNext一致（RightNext）
		Right.transform.position = PageBack.transform.position; 
        Right.transform.eulerAngles = new Vector3(0, 0, 0);
        Right.sprite = bookPages[1];
		PageBack.sprite = bookPages[2];
        //LeftNext.transform.SetAsFirstSibling();

        if (enableShadowEffect) Shadow.gameObject.SetActive(true);
        UpdateBookRTLToPoint(f);
    }

	//正在拖拽右边页面
    public void OnMouseDragRightPage()
    {
        if (interactable)
        DragRightPageToPoint(transformPoint(Input.mousePosition));        
    }

    public void DragLeftPageToPoint(Vector3 point)
    {
//		if (currentPage >= bookPages.Length)
//			return;

        pageDragging = true;
        mode = FlipMode.LeftToRight;
        f = point;

        NextPageClip.rectTransform.pivot = new Vector2(1, 0.12f);
        ClippingPlane.rectTransform.pivot = new Vector2(0, 0.35f);

        Right.gameObject.SetActive(true);
        Right.transform.position = PageBack.transform.position;
        Right.sprite = bookPages[0];
        Right.transform.eulerAngles = new Vector3(0, 0, 0);
        Right.transform.SetAsFirstSibling();

        Left.gameObject.SetActive(true);
//        Left.rectTransform.pivot = new Vector2(1, 0);
        Left.transform.position = PageBack.transform.position;
        Left.transform.eulerAngles = new Vector3(0, 0, 0);
		Left.sprite = bookPages[1];

        PageBack.transform.SetAsFirstSibling();
        if (enableShadowEffect) ShadowLTR.gameObject.SetActive(true);

        UpdateBookLTRToPoint(f);
    }

	//鼠标拖拽左边页面
    public void OnMouseDragLeftPage()
    {
		if (interactable) {
			DragLeftPageToPoint(transformPoint(Input.mousePosition));
		}        
    }

	//鼠标松开
    public void OnMouseRelease()
    {
        if (interactable)
        {
            ReleasePage(IfMouseBeyond());
        }
            
	}

	//释放页面
    public void ReleasePage(int flagS)
    {
        //return;    
        if (pageDragging)
        {
            pageDragging = false;
            // TweenBack返回原来的位置；TweenForward上翻
            if (flagS == 0)
                TweenBack();
            else if(flagS == 1)
                TweenForward();
        }
    }
    //判断鼠标点是否超出范围
    /*
     * 令矢量的起点为A，终点为B，判断的点为C
     * 如果S为正数，则C在矢量AB的左侧
     * 如果S为负数，则C在矢量AB的右侧
     * 如果S为0，则C在直线AB上
     */
    int CountS(Vector3 origin, Vector3 destination, Vector3 point) {
        int s;
        Vector3 A = origin;
        Vector3 B = destination;
        Vector3 C = point;
        s = (int)((A.x - C.x) * (B.y - C.y) - (A.y - C.y) * (B.x - C.x));
        return s;
    }
    //判断是否超出
    //返回值：0 没有超出，1 超出
    public int IfMouseBeyond()
    {
        int flagS = 0;

        //初始参数是LTR模式
        Vector3 Calc_SB = st;
        Vector3 Calc_EBT = ebr;
        //模式不同，判断线不同
        if (mode == FlipMode.RightToLeft)
        {
            Calc_SB = sb;
            Calc_EBT.x = -Calc_EBT.x;
        }
        //flagS = CountS(Calc_EBT, Calc_SB, f);
        //以上，为斜线判定

        if (c.y > ebr.y / 2)
            flagS = 1;
        else
            flagS = 0;

       // if (mode == FlipMode.LeftToRight) flagS = -1;
       
        return flagS;
    }

    Coroutine currentCoroutine;

	//更换图片
    //翻开
    void UpdateSpritesForward()
    {
		PageBack.sprite = bookPages [1];
    }
    //不翻开
	void UpdateSpritesBack()
	{
		//LeftNext.sprite = bookPages [2];
		PageBack.sprite = bookPages [0];
	}

    //翻开的效果
    public void TweenForward()
    { 
		if (mode == FlipMode.RightToLeft)	{		
//			Debug.Log ("this is TweenForward Begin");
			//TweenToForward参数说明: 1.摆正page的位置点；2.持续时间 
			currentCoroutine = StartCoroutine ( TweenToForward (sb, 0.5f, () => {Flip ();}));
		}
        else
            currentCoroutine = StartCoroutine ( TweenToForward (st, 0.5f, () => { Flip();}));
    }

    //？
    void Flip()
    {
        PageBack.transform.SetParent(BookPanel.transform, true);
		Right.transform.SetParent(BookPanel.transform, true);
		Left.gameObject.SetActive(false);
		Right.gameObject.SetActive(false);
        //LeftNext.transform.SetParent(BookPanel.transform, true);
        Left.transform.SetParent(BookPanel.transform, true);
        Shadow.gameObject.SetActive(false);
		ShadowLTR.gameObject.SetActive(false);
        if (mode == FlipMode.LeftToRight) { }
        else if (mode == FlipMode.RightToLeft) { }
		UpdateSpritesForward();
		pageDragging = false;

        if (OnFlip != null)
            OnFlip.Invoke();
    }
	//
    public void TweenBack()
    {
        if (mode == FlipMode.RightToLeft)
        {
            currentCoroutine = StartCoroutine(TweenTo(ebr,0.15f,
				() =>
                {
//					Debug.Log("Test: this is RTL TweenBack.");
					UpdateSpritesBack();
                    PageBack.transform.SetParent(BookPanel.transform);
                    Right.transform.SetParent(BookPanel.transform);
                    Left.gameObject.SetActive(false);
                    Right.gameObject.SetActive(false);
                    pageDragging = false;
                }
                ));
        }
        else if(mode == FlipMode.LeftToRight)
        {
            currentCoroutine = StartCoroutine(TweenTo(ebt, 0.15f,
                () =>
                {
					UpdateSpritesBack();
                    //LeftNext.transform.SetParent(BookPanel.transform);
                    Left.transform.SetParent(BookPanel.transform);
                    Left.gameObject.SetActive(false);
                    Right.gameObject.SetActive(false);
                    pageDragging = false;
                }
                ) );
        }
    }

    public IEnumerator TweenTo(Vector3 to, float duration, System.Action onFinish)
    {
        int steps = (int)(duration / 0.025f);
		//0.025 = 1/40
        Vector3 displacement = (to - f) / steps;
        for (int i = 0; i < steps -1; i++)
        {
            if(mode== FlipMode.RightToLeft)
            UpdateBookRTLToPoint(f + displacement);
            else
            UpdateBookLTRToPoint(f + displacement);

            yield return new WaitForSeconds(0.025f);
        }
        if (onFinish != null)
            onFinish();
	} 
    //参数说明:1.目标点；2.持续时间；3.结束后要做的
	public IEnumerator TweenToForward(Vector3 to, float duration, System.Action onFinish)
	{
		//步数；duration：持续时间
		int steps = (int)(duration / 0.025f);
		//每一步的位移距离   
		Vector3 displacement = (to - f) / steps;
		for (int i = 0; i < steps; i++)
		{	
//			Debug.Log ("F :"+ f);
			if (mode == FlipMode.RightToLeft)
				UpdateBookRTLToPoint (f + displacement);
			else if(mode == FlipMode.LeftToRight)
				UpdateBookLTRToPoint(f + displacement);
			yield return new WaitForSeconds(0.025f);
		}

		displacement = (ebr - to) / steps;
//		f = sb; 
		//展开效果
		for (int i = 0; i < steps ; i++) {
//			Debug.Log ("第"+i+"步");
//			Debug.Log ("F :"+ f);
			//这里的 f重复的更新		
			UpdateBookUnfold (f + displacement);
			yield return new WaitForSeconds(0.025f);
		}

		if (onFinish != null)
			onFinish();
	}

	//回正
	public void UpdateBookUnfold(Vector3 followLocation)
	{   
		f = followLocation;

        Vector3 t1;
        float T0_T1_Angle;
        float C_T1_dy;
        float C_T1_dx;
        float C_T1_Angle;

        if (mode == FlipMode.RightToLeft)
        {
            //左边点
            Vector3 RightWord = Right.transform.position;
            Shadow.transform.SetParent(ClippingPlane.transform, true);
            Shadow.transform.localPosition = new Vector3(0, 0, 0);
            Shadow.transform.localEulerAngles = new Vector3(0, 0, 0);

            Right.transform.SetParent(ClippingPlane.transform, true);
            Left.transform.SetParent(BookPanel.transform, true);
            Left.gameObject.SetActive(false);
            PageBack.transform.SetParent(BookPanel.transform, true);
            //c = Calc_C_Position(followLocation);
            c = followLocation;
   
            T0_T1_Angle = Calc_T0_T1_Angle(c, ebr, out t1);

            T0_T1_Angle = -180;
            ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);
            ClippingPlane.transform.eulerAngles = new Vector3(0, 0, T0_T1_Angle + 90);
            ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

            Right.transform.position = RightWord;

            C_T1_dy = t1.y - c.y;
            C_T1_dx = t1.x - c.x;
            C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;

            Right.transform.eulerAngles = new Vector3(0, 0, C_T1_Angle + 90);

            NextPageClip.transform.eulerAngles = new Vector3(0, 0, T0_T1_Angle + 90);
            NextPageClip.transform.position = BookPanel.TransformPoint(t1);

            //RightNext.transform.SetParent(NextPageClip.transform, true);
            //Left.transform.SetParent(ClippingPlane.transform, true);
            //Left.transform.SetAsFirstSibling();
            Shadow.rectTransform.SetParent(Right.rectTransform, true);
        }
        else if(mode == FlipMode.LeftToRight) {
        //右边点
}
        Vector3 LeftWord = Right.transform.position;

        ShadowLTR.transform.SetParent(NextPageClip.transform, true);
        ShadowLTR.transform.localPosition = new Vector3(0, 0, 0);
        ShadowLTR.transform.localEulerAngles = new Vector3(0, 0, 0);

        Left.transform.SetParent(ClippingPlane.transform, true);
        Right.transform.SetParent(BookPanel.transform, true);
        //LeftNext.transform.SetParent(BookPanel.transform, true);
        c = followLocation;

        T0_T1_Angle = Calc_T0_T1_Angle(c, ebl, out t1);
        T0_T1_Angle = -180;

        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);
        ClippingPlane.transform.eulerAngles = new Vector3(0, 0, T0_T1_Angle - 90);//遮罩
        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);
        Left.transform.position = LeftWord;

        NextPageClip.transform.eulerAngles = new Vector3(0, 0, T0_T1_Angle + 90);//隐藏
        NextPageClip.transform.position = BookPanel.TransformPoint(t1);

        C_T1_dy = t1.y - c.y;
        C_T1_dx = t1.x - c.x;
        C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;

        //Left.transform.position = BookPanel.TransformPoint(c);
        Left.transform.eulerAngles = new Vector3(0, 0, C_T1_Angle - 90);//显示的牌
        
        //LeftNext.transform.SetParent(NextPageClip.transform, true);
        Right.transform.SetParent(ClippingPlane.transform, true);
        Right.transform.SetAsFirstSibling();
        ShadowLTR.rectTransform.SetParent(Left.rectTransform, true);

    }
}
