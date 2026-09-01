#!/usr/bin/env python3
"""Detect and perspective-rectify a photographed sheet of paper."""

import argparse
import sys
from pathlib import Path
from typing import Optional, Tuple

import cv2
import numpy as np


CornerArray = np.ndarray


def order_corners(corners: CornerArray) -> CornerArray:
    points = np.asarray(corners, dtype=np.float32).reshape(4, 2)
    sums = points.sum(axis=1)
    differences = np.diff(points, axis=1).reshape(-1)
    return np.float32(
        [
            points[np.argmin(sums)],
            points[np.argmin(differences)],
            points[np.argmax(sums)],
            points[np.argmax(differences)],
        ]
    )


def detect_paper_corners(image: np.ndarray) -> Optional[CornerArray]:
    if image is None or image.size == 0:
        return None

    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    blurred = cv2.GaussianBlur(gray, (5, 5), 0)
    edges = cv2.Canny(blurred, 50, 150)
    edges = cv2.dilate(edges, np.ones((3, 3), np.uint8), iterations=1)
    contours, _ = cv2.findContours(edges, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    minimum_area = image.shape[0] * image.shape[1] * 0.15

    for contour in sorted(contours, key=cv2.contourArea, reverse=True):
        if cv2.contourArea(contour) < minimum_area:
            break
        perimeter = cv2.arcLength(contour, True)
        approximation = cv2.approxPolyDP(contour, 0.02 * perimeter, True)
        if len(approximation) == 4 and cv2.isContourConvex(approximation):
            return order_corners(approximation.reshape(4, 2))

    return None


def warp_paper(image: np.ndarray, corners: CornerArray) -> np.ndarray:
    top_left, top_right, bottom_right, bottom_left = order_corners(corners)
    width = max(
        np.linalg.norm(top_right - top_left),
        np.linalg.norm(bottom_right - bottom_left),
    )
    height = max(
        np.linalg.norm(bottom_left - top_left),
        np.linalg.norm(bottom_right - top_right),
    )
    output_width = max(1, int(round(width)))
    output_height = max(1, int(round(height)))
    destination = np.float32(
        [
            [0, 0],
            [output_width - 1, 0],
            [output_width - 1, output_height - 1],
            [0, output_height - 1],
        ]
    )
    transform = cv2.getPerspectiveTransform(
        np.float32([top_left, top_right, bottom_right, bottom_left]), destination
    )
    return cv2.warpPerspective(image, transform, (output_width, output_height))


def rectify(
    image: np.ndarray, corners: Optional[CornerArray] = None
) -> Tuple[np.ndarray, CornerArray]:
    detected = detect_paper_corners(image) if corners is None else order_corners(corners)
    if detected is None:
        raise ValueError("NEEDS_CORNER_CONFIRMATION")
    return warp_paper(image, detected), detected


def parse_corners(value: str) -> CornerArray:
    try:
        points = [tuple(float(number) for number in pair.split(",")) for pair in value.split(";")]
    except ValueError as error:
        raise argparse.ArgumentTypeError("corners must contain numeric x,y pairs") from error
    if len(points) != 4 or any(len(point) != 2 for point in points):
        raise argparse.ArgumentTypeError("corners must be 'x,y;x,y;x,y;x,y'")
    return order_corners(np.float32(points))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--corners", type=parse_corners)
    arguments = parser.parse_args()

    image = cv2.imread(str(arguments.input), cv2.IMREAD_COLOR)
    if image is None:
        print(f"Unable to read input image: {arguments.input}", file=sys.stderr)
        return 1

    try:
        result, used_corners = rectify(image, arguments.corners)
    except ValueError as error:
        print(str(error), file=sys.stderr)
        return 2

    arguments.output.parent.mkdir(parents=True, exist_ok=True)
    if not cv2.imwrite(str(arguments.output), result):
        print(f"Unable to write output image: {arguments.output}", file=sys.stderr)
        return 1

    formatted = ";".join(f"{point[0]:.1f},{point[1]:.1f}" for point in used_corners)
    print(f"RECTIFIED {result.shape[1]}x{result.shape[0]} corners={formatted}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
