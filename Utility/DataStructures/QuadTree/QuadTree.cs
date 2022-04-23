using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class QuadTree<T> 
{
    public QuadTree<T> Root { get {if(Parent != null) return Parent.Root; return this;} }
    public QuadTree<T> Parent { get; private set; }
    public bool IsLeaf { get; private set; }
    public bool HasElement { get; private set; }
    public int Level { get; private set; }
    public List<T> Elements { get; private set; } 
    //public T Element { get; private set; } 
    public List<QuadTree<T>> Children { get; private set; }  
    private QuadTree<T> _tlChild, _trChild, _blChild, _brChild; 
    public Rectangle _bounds; 
    private Func<T, Vector2> _elementPos; 

    public QuadTree(int maxElementsInCell, List<T> elements, Func<T, Vector2> elementPos, Rectangle bounds, QuadTree<T> parent = null)
    {
        Parent = parent; 
        if(Parent == null) Level = 0;
        else Level = Parent.Level + 1;
        elements = new List<T>(elements);
        _bounds = bounds;
        _elementPos = elementPos;
        Elements = new List<T>();

        if(elements.Count == 0)
        {
            //Element = default(T);
            HasElement = false; 
            IsLeaf = true; 
        }
        else if(elements.Count <= maxElementsInCell)
        {
            IsLeaf = true; 

            for (int i = 0; i < elements.Count; i++)
            {
                if(elements[i].Equals(default(T)) == false)
                {
                    HasElement = true; 
                    //Element = elements[0];
                    Elements.Add(elements[i]);
                }
            }
        }
        else if(elements.Count > maxElementsInCell)
        {
            Elements = elements.ToList();

            //Element = default(T);

            IsLeaf = false; 

            var newBounds = _bounds.Divide();

            var tlElements = elements.Where(e => newBounds[0].Contains(elementPos(e))).ToList();
            elements = elements.Except(tlElements).ToList();

            var trElements = elements.Where(e => newBounds[1].Contains(elementPos(e))).ToList();
            elements = elements.Except(trElements).ToList();

            var blElements = elements.Where(e => newBounds[2].Contains(elementPos(e))).ToList();
            elements = elements.Except(blElements).ToList();

            var brElements = elements.Where(e => newBounds[3].Contains(elementPos(e))).ToList();
            elements = elements.Except(brElements).ToList();

            if(elements.Count > 0)
            {
                throw new Exception("not all elements accounted for");
            }

            _tlChild = new QuadTree<T>(maxElementsInCell, tlElements, elementPos, newBounds[0], this);
            _trChild = new QuadTree<T>(maxElementsInCell, trElements, elementPos, newBounds[1], this);
            _blChild = new QuadTree<T>(maxElementsInCell, blElements, elementPos, newBounds[2], this);
            _brChild = new QuadTree<T>(maxElementsInCell, brElements, elementPos, newBounds[3], this);
            Children = new List<QuadTree<T>>(){_tlChild, _trChild, _blChild, _brChild};
            
            HasElement = true;
        }
    }

    public QuadTree<T> GetCell(Vector2 point, int maxLevel = int.MaxValue)
    {
        if(_bounds.Contains(point) == false)
        {
            if(Parent == null) return null;
            return Parent.GetCell(point, maxLevel);
        }
        if(IsLeaf || Level == maxLevel)
        {
            return this;
        }

        var child = Children.Where(c => c._bounds.Contains(point)).First();
        return child.GetCell(point, maxLevel);
    }
    public QuadTree<T> GetSmallestElementHavingCell(Vector2 point, int maxLevel = int.MaxValue)
    {
        if(_bounds.Contains(point) == false)
        {
            //GD.Print("out of bounds");
            if(Parent == null) return null;
            return Parent.GetSmallestElementHavingCell(point, maxLevel);
        }
        if(HasElement)
        {
            if(IsLeaf || Level == maxLevel)
            {
                return this;
            }
            else
            {
                //GD.Print("looking for children with element");

                var child = Children.Where(c => c._bounds.Contains(point) && c.HasElement).FirstOrDefault();
                if(child == null) return this;
                return child.GetSmallestElementHavingCell(point, maxLevel);
            }
        }
        else 
        {
            //GD.Print("no element, moving up");

            if(Parent == null) return null;
            return Parent.GetSmallestElementHavingCell(point, maxLevel);
        }
    }

    public T GetClosestElement(Vector2 point)
    {
        if(_bounds.Contains(point) == false)
        {
            GD.Print("point is out of bounds, returning null");
            return default(T);
        }
        var current = GetSmallestElementHavingCell(point);
        if(current == null) 
        {
            GD.Print("could not find smallest element having cell, returning null");
            return default(T);
        }
        else if(current.HasElement == false)
        {
            GD.Print("smallest element having cell did not have element, returning null");
            return default(T);
        }

        
        T e = current.Elements.OrderBy(t => _elementPos(t).DistanceSquaredTo(point)).First();

        var ePos = _elementPos(e);
        float maxDist = point.DistanceTo(ePos);
        
        var tl = new Vector2(point.x - maxDist, point.y - maxDist);
        var br = new Vector2(point.x + maxDist, point.y + maxDist);
        var box = new Rectangle(tl, br);
        


        
        var list = Root.GetAllElementsInBounds(box);
        if(list.Count == 0)
        {
            /*
            GD.Print("element position: " + ePos);
            GD.Print("query point: " + point);
            GD.Print("root elements: " + Root.Elements.Count);
            GD.Print("All elements in box returned none, returning null");
            */
            return e;
        }
        
        return list.OrderBy( f => _elementPos(f).DistanceSquaredTo(point)).First();
    }

    public List<T> GetAllElementsInBounds(Rectangle bounds)
    {
        var elements = new List<T>();
        if(HasElement)
        {
            if(IsLeaf)
            {
                elements.AddRange(Elements.Where(e => bounds.Contains(_elementPos(e))));
            }
            else
            {
                foreach (var c in Children)
                {
                    elements.AddRange(c.GetAllElementsInBounds(bounds));
                }
            }
        }
        return elements;
    }
}


public struct Rectangle
{
    public Vector2 TL, BR; 
    public Vector2 Middle { get; private set; }
    public float Width { get; private set; }
    public float Height { get; private set; }
    public Rectangle(Vector2 topLeftCorner, Vector2 bottomRightCorner)
    {
        TL = topLeftCorner; 
        BR = bottomRightCorner;
        Width = BR.x - TL.x;
        Height = BR.y - TL.y;
        Middle = (TL + BR) / 2f; 
    }
    public bool Contains(Vector2 point)
    {
        if(TL.x <= point.x && BR.x >= point.x)
        {
            if(TL.y <= point.y && BR.y >= point.y)
            {
                return true; 
            }
        }
        return false; 
    }

    public bool Overlaps(Rectangle rec)
    {
        if(Contains(rec.TL)) return true;
        if(Contains(rec.BR)) return true;
        if(rec.Contains(TL)) return true;
        if(rec.Contains(BR)) return true;
        return false; 
    }

    public List<Rectangle> Divide()
    {
        var midX = (TL.x + BR.x) / 2f;
        var midY =  (TL.y + BR.y) / 2f;

        var topLeft = new Vector2(TL.x, TL.y);
        var topMiddle = new Vector2( midX , TL.y);
        var topRight = new Vector2(BR.x, TL.y);

        var midLeft = new Vector2(TL.x, midY );
        var midMiddle = new Vector2(midX , midY );
        var midRight = new Vector2(BR.x, midY );

        var bottomLeft = new Vector2(TL.x, BR.y);
        var bottomMiddle = new Vector2(midX , BR.y);
        var bottomRight = new Vector2(BR.x, BR.y);

        var topLeftRec = new Rectangle(topLeft, midMiddle);
        var topRightRec = new Rectangle(topMiddle, midRight);
        var bottomLeftRec = new Rectangle(midLeft, bottomMiddle);
        var bottomRightRec = new Rectangle(midMiddle, bottomRight);

        var result = new List<Rectangle>(){topLeftRec, topRightRec, bottomLeftRec, bottomRightRec};

        return result; 
    }
}
